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
  templateUrl: './patient-detail.component.html',
  styleUrls: ['./patient-detail.component.scss']
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
    const mrn = this.route.snapshot.paramMap.get('mrn')!;
    this.patientSvc.getByMrn(mrn).subscribe({
      next: p => {
        this.patient = p;
        this.loading = false;
        this.loadCases(p.mrn);
      },
      error: () => {
        this.snack.open('Patient not found.', 'Close', { duration: 4000 });
        this.router.navigate(['/patients']);
      }
    });
  }

  private loadCases(mrn: string): void {
    this.caseSvc.getByPatientMrn(mrn).subscribe({
      next: c => { this.cases = c; this.casesLoading = false; },
      error: () => { this.casesLoading = false; }
    });
  }

}
