import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { AppointmentService } from '../appointment.service';
import { AppointmentDto } from '../../../core/models/appointment.models';
import { AuthService } from '../../../core/services/auth.service';

// Colour map for status text. Backgrounds are generated in statusBg()
const STATUS_COLORS: Record<string, string> = {
  Scheduled: '#1976d2',
  Confirmed: '#388e3c',
  Completed: '#6a1b9a',
  NoShow: '#e65100',
  Cancelled: '#757575'
};

@Component({
  selector: 'app-appointment-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatPaginatorModule,
    MatChipsModule,
    MatSnackBarModule
  ],
  templateUrl: './appointment-list.component.html',
  styleUrls: ['./appointment-list.component.scss']
})
export class AppointmentListComponent implements OnInit {
  appointments: AppointmentDto[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 20;
  loading = false;
  statuses = ['Scheduled', 'Confirmed', 'Completed', 'NoShow', 'Cancelled'];
  columns = ['patient', 'provider', 'type', 'scheduledStart', 'status', 'actions'];
  statusFilter = new FormControl('');

  get canSchedule(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator') || this.auth.hasRole('Supervisor');
  }

  constructor(
    public router: Router,
    private svc: AppointmentService,
    private auth: AuthService,
    private snack: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.load();
    this.statusFilter.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => { this.page = 1; this.load(); });
  }

  load(): void {
    this.loading = true;
    this.svc.list(this.page, this.pageSize, this.statusFilter.value || undefined).subscribe({
      next: res => {
        this.appointments = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: () => {
        this.snack.open('Failed to load appointments.', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  onPage(e: PageEvent): void {
    this.page = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.load();
  }

  // Light background tint paired with the STATUS_COLORS text colour
  statusBg(status: string): string {
    const map: Record<string, string> = {
      Scheduled: '#e3f2fd',
      Confirmed: '#e8f5e9',
      Completed: '#f3e5f5',
      NoShow: '#fff3e0',
      Cancelled: '#f5f5f5'
    };
    return map[status] ?? '#f5f5f5';
  }

  statusColor(status: string): string {
    return STATUS_COLORS[status] ?? '#757575';
  }
}
