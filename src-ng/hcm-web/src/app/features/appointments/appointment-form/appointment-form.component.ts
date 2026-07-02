import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AppointmentService } from '../appointment.service';
import { ProviderDto } from '../../../core/models/appointment.models';

const APPOINTMENT_TYPES = ['Initial Assessment', 'Follow-Up', 'Specialist Referral', 'Telehealth'];

@Component({
  selector: 'app-appointment-form',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatSnackBarModule
  ],
  templateUrl: './appointment-form.component.html',
  styleUrls: ['./appointment-form.component.scss']
})
export class AppointmentFormComponent implements OnInit {
  form: FormGroup;
  providers: ProviderDto[] = [];
  types = APPOINTMENT_TYPES;
  saving = false;
  conflictError = '';

  constructor(
    private fb: FormBuilder,
    private svc: AppointmentService,
    private router: Router,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      patientMrn: ['', Validators.required],
      caseNumber: [''],
      providerId: ['', Validators.required],
      appointmentType: ['', Validators.required],
      scheduledStart: ['', Validators.required],
      scheduledEnd: ['', Validators.required],
      location: [''],
      notes: ['']
    });
  }

  ngOnInit(): void {
    this.svc.getProviders().subscribe({ next: p => this.providers = p, error: () => {} });
  }

  submit(): void {
    if (this.form.invalid || this.saving) return;
    this.saving = true;
    this.conflictError = '';

    const val = this.form.value;
    this.svc.create({
      patientMrn: val.patientMrn,
      caseNumber: val.caseNumber || undefined,
      providerId: val.providerId,
      appointmentType: val.appointmentType,
      scheduledStart: new Date(val.scheduledStart).toISOString(),
      scheduledEnd: new Date(val.scheduledEnd).toISOString(),
      location: val.location || undefined,
      notes: val.notes || undefined
    }).subscribe({
      next: appt => {
        this.snack.open('Appointment scheduled.', 'Close', { duration: 3000 });
        this.router.navigate(['/appointments', appt.id]);
      },
      error: err => {
        this.saving = false;
        const msg = err.error?.error ?? 'Failed to schedule appointment.';
        if (msg.toLowerCase().includes('already has an appointment')) {
          this.conflictError = msg;
        } else {
          this.snack.open(msg, 'Close', { duration: 4000 });
        }
      }
    });
  }
}
