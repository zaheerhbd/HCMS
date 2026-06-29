import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { PatientService } from '../services/patient.service';
import { CaseService } from '../../cases/services/case.service';
import { PatientDto } from '../../../core/models/patient.models';
import { CaseListItemDto } from '../../../core/models/case.models';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatTabsModule,
    MatExpansionModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatDividerModule,
    MatSnackBarModule,
    MatTableModule,
    StatusBadgeComponent
  ],
  template: `
    <div *ngIf="loading" class="spinner-wrap"><mat-spinner diameter="40"></mat-spinner></div>

    <ng-container *ngIf="patient && !loading">
      <!-- Breadcrumb -->
      <nav class="breadcrumb">
        <a routerLink="/patients">Patients</a> /
        <span>{{ patient.firstName }} {{ patient.lastName }}</span>
      </nav>

      <!-- Header -->
      <div class="page-header">
        <div>
          <h1>{{ patient.firstName }} {{ patient.lastName }}</h1>
          <span class="mrn-badge">{{ patient.mrn }}</span>
        </div>
        <div class="header-actions">
          <button mat-stroked-button [routerLink]="['edit']" *ngIf="canEdit">
            <mat-icon>edit</mat-icon> Edit
          </button>
          <button mat-flat-button color="primary" routerLink="/cases/new"
            [queryParams]="{ patientId: patient.id }" *ngIf="canCreateCase">
            <mat-icon>folder_open</mat-icon> New Case
          </button>
        </div>
      </div>

      <mat-tab-group>
        <!-- Demographics Tab -->
        <mat-tab label="Demographics">
          <div class="tab-content">
            <mat-card>
              <mat-card-content>
                <div class="field-grid">
                  <div class="field"><label>Date of Birth</label><span>{{ patient.dateOfBirth | date:'mediumDate' }}</span></div>
                  <div class="field"><label>Gender</label><span>{{ patient.gender ?? '—' }}</span></div>
                  <div class="field"><label>Phone</label><span>{{ patient.phone ?? '—' }}</span></div>
                  <div class="field"><label>Email</label><span>{{ patient.email ?? '—' }}</span></div>
                  <div class="field"><label>Address</label><span>{{ fullAddress }}</span></div>
                  <div class="field"><label>Emergency Contact</label><span>{{ emergencyContact }}</span></div>
                </div>
              </mat-card-content>
            </mat-card>

            <!-- Insurance accordion -->
            <h3 style="margin-top:24px">Insurance</h3>
            <div *ngIf="patient.insurance.length === 0" class="empty-state">No insurance records.</div>
            <mat-accordion>
              <mat-expansion-panel *ngFor="let ins of patient.insurance">
                <mat-expansion-panel-header>
                  <mat-panel-title>{{ ins.insurancePlan }}</mat-panel-title>
                  <mat-panel-description>
                    Member ID: {{ ins.memberId }}
                    <span *ngIf="ins.isPrimary" class="primary-badge">Primary</span>
                  </mat-panel-description>
                </mat-expansion-panel-header>
                <div class="field-grid">
                  <div class="field"><label>Group Number</label><span>{{ ins.groupNumber ?? '—' }}</span></div>
                  <div class="field"><label>Subscriber</label><span>{{ ins.subscriberName ?? '—' }}</span></div>
                  <div class="field"><label>Effective</label><span>{{ ins.effectiveDate | date:'mediumDate' }}</span></div>
                  <div class="field"><label>Terminates</label><span>{{ ins.terminationDate ? (ins.terminationDate | date:'mediumDate') : 'Active' }}</span></div>
                </div>
              </mat-expansion-panel>
            </mat-accordion>
          </div>
        </mat-tab>

        <!-- Cases Tab -->
        <mat-tab label="Cases ({{ cases.length }})">
          <div class="tab-content">
            <div *ngIf="casesLoading" class="spinner-wrap"><mat-spinner diameter="32"></mat-spinner></div>
            <div *ngIf="!casesLoading && cases.length === 0" class="empty-state">No cases yet.</div>
            <table mat-table [dataSource]="cases" *ngIf="!casesLoading && cases.length > 0">
              <ng-container matColumnDef="caseNumber">
                <th mat-header-cell *matHeaderCellDef>Case #</th>
                <td mat-cell *matCellDef="let c">
                  <a [routerLink]="['/cases', c.id]">{{ c.caseNumber }}</a>
                </td>
              </ng-container>
              <ng-container matColumnDef="caseTypeName">
                <th mat-header-cell *matHeaderCellDef>Type</th>
                <td mat-cell *matCellDef="let c">{{ c.caseTypeName }}</td>
              </ng-container>
              <ng-container matColumnDef="currentStatus">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let c">
                  <app-status-badge [status]="c.currentStatus"></app-status-badge>
                </td>
              </ng-container>
              <ng-container matColumnDef="openedAt">
                <th mat-header-cell *matHeaderCellDef>Opened</th>
                <td mat-cell *matCellDef="let c">{{ c.openedAt | date:'mediumDate' }}</td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="caseCols"></tr>
              <tr mat-row *matRowDef="let row; columns: caseCols;"
                class="clickable-row" [routerLink]="['/cases', row.id]"></tr>
            </table>
          </div>
        </mat-tab>

        <!-- Documents Tab (placeholder) -->
        <mat-tab label="Documents">
          <div class="tab-content empty-state">
            Document management coming in Phase 3.
          </div>
        </mat-tab>
      </mat-tab-group>
    </ng-container>
  `,
  styles: [`
    .breadcrumb { margin-bottom: 12px; font-size: 14px; color: #757575; }
    .breadcrumb a { color: #1976d2; text-decoration: none; }
    .page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; }
    .header-actions { display: flex; gap: 8px; }
    .mrn-badge { background: #e3f2fd; color: #1976d2; padding: 2px 8px; border-radius: 12px; font-size: 13px; }
    .primary-badge { background: #e8f5e9; color: #388e3c; padding: 1px 6px; border-radius: 10px; font-size: 12px; margin-left: 8px; }
    .tab-content { padding: 20px 0; }
    .field-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .field label { display: block; font-size: 12px; color: #757575; margin-bottom: 2px; }
    .field span { font-size: 15px; }
    .spinner-wrap { display: flex; justify-content: center; padding: 40px; }
    .empty-state { text-align: center; padding: 40px; color: #757575; }
    .clickable-row { cursor: pointer; }
    table { width: 100%; }
  `]
})
export class PatientDetailComponent implements OnInit {
  patient: PatientDto | null = null;
  cases: CaseListItemDto[] = [];
  loading = true;
  casesLoading = true;
  caseCols = ['caseNumber', 'caseTypeName', 'currentStatus', 'openedAt'];

  get canEdit(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator');
  }

  get canCreateCase(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator');
  }

  get fullAddress(): string {
    const p = this.patient!;
    const parts = [p.address, p.city, p.state, p.zipCode].filter(Boolean);
    return parts.join(', ') || '—';
  }

  get emergencyContact(): string {
    const p = this.patient!;
    if (!p.emergencyContactName) return '—';
    return p.emergencyContactPhone
      ? `${p.emergencyContactName} (${p.emergencyContactPhone})`
      : p.emergencyContactName;
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private patientSvc: PatientService,
    private caseSvc: CaseService,
    private snack: MatSnackBar,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.patientSvc.getById(id).subscribe({
      next: p => {
        this.patient = p;
        this.loading = false;
        this.loadCases(p.id);
      },
      error: () => {
        this.snack.open('Patient not found.', 'Close', { duration: 4000 });
        this.router.navigate(['/patients']);
      }
    });
  }

  private loadCases(patientId: string): void {
    this.caseSvc.getByPatient(patientId).subscribe({
      next: c => { this.cases = c; this.casesLoading = false; },
      error: () => { this.casesLoading = false; }
    });
  }
}
