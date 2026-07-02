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
import { CaseTasksComponent } from '../tasks/tasks.component';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { AuthService } from '../../../core/services/auth.service';

// Valid next statuses per server-side state machine — mirrored here for UI hints only
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
    CaseNotesComponent,
    CaseTasksComponent
  ],
  templateUrl: './case-detail.component.html',
  styleUrls: ['./case-detail.component.scss']
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
    const caseNumber = this.route.snapshot.paramMap.get('caseNumber')!;
    this.svc.getById(caseNumber).subscribe({
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
    this.svc.changeStatus(this.c.caseNumber, { newStatus: this.statusSelect.value }).subscribe({
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
      this.svc.close(this.c.caseNumber).subscribe({
        next: updated => {
          this.c = updated;
          this.snack.open('Case closed.', 'Close', { duration: 3000 });
        },
        error: () => this.snack.open('Failed to close case.', 'Close', { duration: 4000 })
      });
    });
  }
}
