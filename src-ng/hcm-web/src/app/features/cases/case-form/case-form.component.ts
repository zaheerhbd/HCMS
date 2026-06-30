import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CaseService } from '../services/case.service';
import { PatientService } from '../../patients/services/patient.service';
import { CaseTypeDto } from '../../../core/models/case.models';

// Seed CaseTypes are fixed by the seeder — hardcoded here to avoid an extra API call
const DEFAULT_CASE_TYPES: CaseTypeDto[] = [
  { id: 1, name: 'Chronic Disease', isActive: true },
  { id: 2, name: 'Post-Surgery', isActive: true },
  { id: 3, name: 'Mental Health', isActive: true },
  { id: 4, name: 'Preventive', isActive: true },
  { id: 5, name: 'Behavioral', isActive: true }
];

@Component({
  selector: 'app-case-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  template: `
    <nav class="breadcrumb">
      <a routerLink="/cases">Cases</a> / <span>New Case</span>
    </nav>
    <h1>New Case</h1>

    <form [formGroup]="form" (ngSubmit)="submit()" class="case-form">
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Patient MRN</mat-label>
        <input matInput formControlName="patientMrn" placeholder="e.g. MRN-2026-00001" />
        <mat-error *ngIf="form.get('patientMrn')?.hasError('required')">Required</mat-error>
      </mat-form-field>

      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Case Type</mat-label>
        <mat-select formControlName="caseTypeId">
          <mat-option *ngFor="let ct of caseTypes" [value]="ct.id">{{ ct.name }}</mat-option>
        </mat-select>
        <mat-error *ngIf="form.get('caseTypeId')?.hasError('required')">Required</mat-error>
      </mat-form-field>

      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Initial Notes</mat-label>
        <textarea matInput formControlName="notes" rows="4"></textarea>
      </mat-form-field>

      <div class="form-actions">
        <button mat-stroked-button type="button" routerLink="/cases">Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="saving">
          <mat-spinner *ngIf="saving" diameter="18" style="display:inline-block;margin-right:6px"></mat-spinner>
          Create Case
        </button>
      </div>
    </form>
  `,
  styles: [`
    .breadcrumb { margin-bottom: 12px; font-size: 14px; color: #757575; }
    .breadcrumb a { color: #1976d2; text-decoration: none; }
    .case-form { max-width: 600px; }
    .full-width { width: 100%; }
    .form-actions { display: flex; gap: 12px; margin-top: 16px; }
  `]
})
export class CaseFormComponent implements OnInit {
  form: FormGroup;
  caseTypes = DEFAULT_CASE_TYPES;
  saving = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private svc: CaseService,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      patientMrn: ['', Validators.required],
      caseTypeId: ['', Validators.required],
      notes: ['']
    });
  }

  ngOnInit(): void {
    // Pre-fill patientId if navigated from patient detail page
    const patientMrn = this.route.snapshot.queryParamMap.get('patientMrn');
    if (patientMrn) this.form.patchValue({ patientMrn });
  }

  submit(): void {
    if (this.form.invalid || this.saving) return;
    this.saving = true;
    this.svc.create(this.form.value).subscribe({
      next: c => {
        this.snack.open('Case created.', 'Close', { duration: 3000 });
        this.router.navigate(['/cases', c.caseNumber]);
      },
      error: err => {
        const msg = err.error?.error ?? err.error?.errors?.[0] ?? 'Failed to create case.';
        this.snack.open(msg, 'Close', { duration: 5000 });
        this.saving = false;
      }
    });
  }
}
