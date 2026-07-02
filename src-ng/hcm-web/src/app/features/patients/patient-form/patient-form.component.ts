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
  templateUrl: './patient-form.component.html',
  styleUrls: ['./patient-form.component.scss']
})
export class PatientFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  saving = false;
  private patientMrn?: string;

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
    this.patientMrn = this.route.snapshot.paramMap.get('mrn') ?? undefined;
    this.isEdit = !!this.patientMrn && this.patientMrn !== 'new';

    if (this.isEdit && this.patientMrn) {
      this.svc.getByMrn(this.patientMrn).subscribe({
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

    const call = this.isEdit && this.patientMrn
      ? this.svc.update(this.patientMrn, payload)
      : this.svc.create(payload);

    call.subscribe({
      next: p => {
        this.snack.open(
          this.isEdit ? 'Patient updated.' : 'Patient created.',
          'Close', { duration: 3000 }
        );
        this.router.navigate(['/patients', p.mrn]);
      },
      error: err => {
        const msg = err.error?.error ?? err.error?.errors?.[0] ?? 'Save failed.';
        this.snack.open(msg, 'Close', { duration: 5000 });
        this.saving = false;
      }
    });
  }
}
