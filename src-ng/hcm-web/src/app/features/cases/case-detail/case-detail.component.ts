import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormControl, Validators } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CaseService } from '../services/case.service';
import { CaseDto } from '../../../core/models/case.models';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge.component';
import { CaseStatusHistoryComponent } from '../case-status-history/case-status-history.component';
import { CareTeamComponent } from '../care-team/care-team.component';
import { CaseNotesComponent } from '../case-notes/case-notes.component';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { AuthService } from '../../../core/services/auth.service';

// Valid transitions per the server-side state machine
const TRANSITIONS: Record<string, string[]> = {
  Open:       ['InProgress', 'OnHold', 'Closed'],
  InProgress: ['OnHold', 'Closed'],
  OnHold:     ['InProgress', 'Closed'],
  Closed:     ['Reopened'],
  Reopened:   ['InProgress', 'OnHold', 'Closed']
};

@Component({
  selector: 'app-case-detail',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatTabsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    StatusBadgeComponent,
    CaseStatusHistoryComponent,
    CareTeamComponent,
    CaseNotesComponent
  ],
  template: `
    <div *ngIf="loading" class="spinner-wrap"><mat-spinner diameter="40"></mat-spinner></div>

    <ng-container *ngIf="c && !loading">
      <!-- Breadcrumb -->
      <nav class="breadcrumb">
        <a routerLink="/cases">Cases</a> /
        <a *ngIf="c.patientId" [routerLink]="['/patients', c.patientId]">Patient</a> /
        <span>{{ c.caseNumber }}</span>
      </nav>

      <!-- Case Header Card -->
      <mat-card class="header-card">
        <mat-card-content>
          <div class="header-row">
            <div>
              <h1 style="margin:0">{{ c.caseNumber }}</h1>
              <div style="margin-top:4px">
                <app-status-badge [status]="c.currentStatus"></app-status-badge>
                <span class="type-label">{{ c.caseTypeName }}</span>
              </div>
            </div>
            <div class="header-actions">
              <!-- Status transition (CareCoordinator, Supervisor, Admin) -->
              <ng-container *ngIf="canChangeStatus && availableTransitions.length">
                <mat-form-field appearance="outline" style="width:160px">
                  <mat-label>Change status</mat-label>
                  <mat-select [formControl]="statusSelect">
                    <mat-option *ngFor="let s of availableTransitions" [value]="s">{{ s }}</mat-option>
                  </mat-select>
                </mat-form-field>
                <button mat-stroked-button (click)="changeStatus()" [disabled]="!statusSelect.value || transitioning">
                  Apply
                </button>
              </ng-container>

              <!-- Close case (Supervisor, Admin only) -->
              <button mat-flat-button color="warn" *ngIf="canClose && c.currentStatus !== 'Closed'"
                (click)="closeCase()">
                <mat-icon>close</mat-icon> Close Case
              </button>
            </div>
          </div>

          <div class="meta-grid" style="margin-top:16px">
            <div><label>Opened</label><span>{{ c.openedAt | date:'mediumDate' }}</span></div>
            <div *ngIf="c.closedAt"><label>Closed</label><span>{{ c.closedAt | date:'mediumDate' }}</span></div>
            <div><label>Assigned To</label><span>{{ c.assignedToUserName ?? 'Unassigned' }}</span></div>
          </div>

          <div *ngIf="c.notes" style="margin-top:12px">
            <label style="font-size:12px;color:#757575">Case Notes</label>
            <p style="margin:4px 0">{{ c.notes }}</p>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Tabs -->
      <mat-tab-group style="margin-top:16px">
        <mat-tab label="Notes">
          <div class="tab-content">
            <app-case-notes [caseId]="c.id"></app-case-notes>
          </div>
        </mat-tab>

        <mat-tab label="Care Team">
          <div class="tab-content">
            <app-care-team [caseId]="c.id"></app-care-team>
          </div>
        </mat-tab>

        <mat-tab label="History">
          <div class="tab-content">
            <app-case-status-history [history]="c.statusHistory"></app-case-status-history>
          </div>
        </mat-tab>

        <mat-tab label="Tasks">
          <div class="tab-content empty">Task management coming in Phase 3.</div>
        </mat-tab>

        <mat-tab label="Documents">
          <div class="tab-content empty">Document management coming in Phase 3.</div>
        </mat-tab>
      </mat-tab-group>
    </ng-container>
  `,
  styles: [`
    .breadcrumb { margin-bottom: 12px; font-size: 14px; color: #757575; }
    .breadcrumb a { color: #1976d2; text-decoration: none; }
    .header-card { margin-bottom: 8px; }
    .header-row { display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 12px; }
    .header-actions { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
    .type-label { margin-left: 10px; font-size: 14px; color: #757575; }
    .meta-grid { display: flex; gap: 32px; flex-wrap: wrap; }
    .meta-grid label { display: block; font-size: 12px; color: #757575; }
    .tab-content { padding: 20px 0; }
    .empty { color: #757575; text-align: center; padding: 40px; }
    .spinner-wrap { display: flex; justify-content: center; padding: 40px; }
  `]
})
export class CaseDetailComponent implements OnInit {
  c: CaseDto | null = null;
  loading = true;
  transitioning = false;
  statusSelect = new FormControl('');

  get canChangeStatus(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator') || this.auth.hasRole('Supervisor');
  }

  get canClose(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('Supervisor');
  }

  get availableTransitions(): string[] {
    if (!this.c) return [];
    return TRANSITIONS[this.c.currentStatus] ?? [];
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private svc: CaseService,
    private dialog: MatDialog,
    private snack: MatSnackBar,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.getById(id).subscribe({
      next: c => { this.c = c; this.loading = false; },
      error: () => {
        this.snack.open('Case not found.', 'Close', { duration: 4000 });
        this.router.navigate(['/cases']);
      }
    });
  }

  changeStatus(): void {
    if (!this.statusSelect.value || !this.c || this.transitioning) return;
    this.transitioning = true;
    this.svc.changeStatus(this.c.id, { newStatus: this.statusSelect.value }).subscribe({
      next: updated => {
        this.c = updated;
        this.statusSelect.reset();
        this.transitioning = false;
        this.snack.open(`Status changed to ${updated.currentStatus}.`, 'Close', { duration: 3000 });
      },
      error: err => {
        this.snack.open(err.error?.error ?? 'Status change failed.', 'Close', { duration: 4000 });
        this.transitioning = false;
      }
    });
  }

  closeCase(): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Close Case', message: 'Are you sure you want to close this case?', confirmLabel: 'Close Case' }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed || !this.c) return;
      this.svc.close(this.c.id).subscribe({
        next: updated => {
          this.c = updated;
          this.snack.open('Case closed.', 'Close', { duration: 3000 });
        },
        error: () => this.snack.open('Failed to close case.', 'Close', { duration: 4000 })
      });
    });
  }
}
