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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { PatientService } from '../services/patient.service';

@Component({
  selector: 'app-patient-form',
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
    MatSnackBarModule,
    MatDatepickerModule,
    MatNativeDateModule
  ],
  template: `
    <nav class="breadcrumb">
      <a routerLink="/patients">Patients</a> /
      <span>{{ isEdit ? 'Edit Patient' : 'New Patient' }}</span>
    </nav>

    <h1>{{ isEdit ? 'Edit Patient' : 'New Patient' }}</h1>

    <form [formGroup]="form" (ngSubmit)="submit()" class="patient-form">
      <h3>Demographics</h3>
      <div class="form-row">
        <mat-form-field appearance="outline">
          <mat-label>First Name</mat-label>
          <input matInput formControlName="firstName" />
          <mat-error *ngIf="form.get('firstName')?.hasError('required')">Required</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Last Name</mat-label>
          <input matInput formControlName="lastName" />
          <mat-error *ngIf="form.get('lastName')?.hasError('required')">Required</mat-error>
        </mat-form-field>
      </div>

      <div class="form-row">
        <mat-form-field appearance="outline">
          <mat-label>Date of Birth</mat-label>
          <input matInput [matDatepicker]="picker" formControlName="dateOfBirth" />
          <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
          <mat-datepicker #picker></mat-datepicker>
          <mat-error *ngIf="form.get('dateOfBirth')?.hasError('required')">Required</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Gender</mat-label>
          <mat-select formControlName="gender">
            <mat-option value="Male">Male</mat-option>
            <mat-option value="Female">Female</mat-option>
            <mat-option value="NonBinary">Non-Binary</mat-option>
            <mat-option value="PreferNotToSay">Prefer Not to Say</mat-option>
          </mat-select>
        </mat-form-field>
      </div>

      <div class="form-row">
        <mat-form-field appearance="outline">
          <mat-label>Phone</mat-label>
          <input matInput formControlName="phone" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Email</mat-label>
          <input matInput formControlName="email" type="email" />
        </mat-form-field>
      </div>

      <h3>Address</h3>
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Street Address</mat-label>
        <input matInput formControlName="address" />
      </mat-form-field>

      <div class="form-row">
        <mat-form-field appearance="outline">
          <mat-label>City</mat-label>
          <input matInput formControlName="city" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>State</mat-label>
          <input matInput formControlName="state" maxlength="2" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Zip Code</mat-label>
          <input matInput formControlName="zipCode" />
        </mat-form-field>
      </div>

      <h3>Emergency Contact</h3>
      <div class="form-row">
        <mat-form-field appearance="outline">
          <mat-label>Contact Name</mat-label>
          <input matInput formControlName="emergencyContactName" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Contact Phone</mat-label>
          <input matInput formControlName="emergencyContactPhone" />
        </mat-form-field>
      </div>

      <div class="form-actions">
        <button mat-stroked-button type="button" routerLink="/patients">Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="saving">
          <mat-spinner *ngIf="saving" diameter="18" style="display:inline-block;margin-right:6px"></mat-spinner>
          {{ isEdit ? 'Save Changes' : 'Create Patient' }}
        </button>
      </div>
    </form>
  `,
  styles: [`
    .breadcrumb { margin-bottom: 12px; font-size: 14px; color: #757575; }
    .breadcrumb a { color: #1976d2; text-decoration: none; }
    .patient-form { max-width: 720px; }
    .form-row { display: flex; gap: 16px; }
    .form-row mat-form-field { flex: 1; }
    .full-width { width: 100%; }
    .form-actions { display: flex; gap: 12px; margin-top: 24px; }
    h3 { margin: 20px 0 8px; color: #424242; font-size: 16px; }
  `]
})
export class PatientFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  saving = false;
  private patientId?: string;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private svc: PatientService,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      dateOfBirth: ['', Validators.required],
      gender: [''],
      phone: [''],
      email: [''],
      address: [''],
      city: [''],
      state: [''],
      zipCode: [''],
      emergencyContactName: [''],
      emergencyContactPhone: ['']
    });
  }

  ngOnInit(): void {
    this.patientId = this.route.snapshot.paramMap.get('id') ?? undefined;
    this.isEdit = !!this.patientId && this.patientId !== 'new';

    if (this.isEdit && this.patientId) {
      this.svc.getById(this.patientId).subscribe({
        next: p => this.form.patchValue({
          ...p,
          dateOfBirth: new Date(p.dateOfBirth)
        }),
        error: () => {
          this.snack.open('Patient not found.', 'Close', { duration: 4000 });
          this.router.navigate(['/patients']);
        }
      });
    }
  }

  submit(): void {
    if (this.form.invalid || this.saving) return;
    this.saving = true;

    const raw = this.form.value;
    const payload = {
      ...raw,
      dateOfBirth: raw.dateOfBirth instanceof Date
        ? raw.dateOfBirth.toISOString().split('T')[0]
        : raw.dateOfBirth
    };

    const call = this.isEdit && this.patientId
      ? this.svc.update(this.patientId, payload)
      : this.svc.create(payload);

    call.subscribe({
      next: p => {
        this.snack.open(
          this.isEdit ? 'Patient updated.' : 'Patient created.',
          'Close', { duration: 3000 }
        );
        this.router.navigate(['/patients', p.id]);
      },
      error: err => {
        const msg = err.error?.error ?? err.error?.errors?.[0] ?? 'Save failed.';
        this.snack.open(msg, 'Close', { duration: 5000 });
        this.saving = false;
      }
    });
  }
}
