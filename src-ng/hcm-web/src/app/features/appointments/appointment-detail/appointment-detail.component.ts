import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { AppointmentService } from '../appointment.service';
import { AppointmentDto } from '../../../core/models/appointment.models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-appointment-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatSnackBarModule
  ],
  templateUrl: './appointment-detail.component.html',
  styleUrls: ['./appointment-detail.component.scss']
})
export class AppointmentDetailComponent implements OnInit {
  appt: AppointmentDto | null = null;
  statusCtrl = new FormControl('');
  saving = false;
  statuses = ['Scheduled', 'Confirmed', 'Completed', 'NoShow', 'Cancelled'];

  get canChangeStatus(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator') || this.auth.hasRole('Supervisor');
  }

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private svc: AppointmentService,
    private auth: AuthService,
    private snack: MatSnackBar
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.getById(id).subscribe({
      next: a => this.appt = a,
      error: () => {
        this.snack.open('Appointment not found.', 'Close', { duration: 3000 });
        this.router.navigate(['/appointments']);
      }
    });
  }

  changeStatus(): void {
    if (!this.statusCtrl.value || !this.appt) return;
    this.saving = true;
    this.svc.changeStatus(this.appt.id, { newStatus: this.statusCtrl.value }).subscribe({
      next: updated => {
        this.appt = updated;
        this.statusCtrl.reset();
        this.saving = false;
        this.snack.open('Status updated.', 'Close', { duration: 3000 });
      },
      error: err => {
        this.snack.open(err.error?.error ?? 'Failed.', 'Close', { duration: 3000 });
        this.saving = false;
      }
    });
  }
}
