import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { CoordinatorDashboardComponent } from './coordinator/coordinator-dashboard.component';
import { SupervisorDashboardComponent } from './supervisor/supervisor-dashboard.component';
import { AdminDashboardComponent } from './admin/admin-dashboard.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, CoordinatorDashboardComponent, SupervisorDashboardComponent, AdminDashboardComponent],
  template: `
    <app-admin-dashboard       *ngIf="role === 'Admin'"></app-admin-dashboard>
    <app-supervisor-dashboard  *ngIf="role === 'Supervisor'"></app-supervisor-dashboard>
    <app-coordinator-dashboard *ngIf="role === 'CareCoordinator' || role === 'Clinician' || role === 'ReadOnly'"></app-coordinator-dashboard>
    <div *ngIf="!role" style="padding:32px;color:#555">Loading dashboard...</div>
  `
})
export class DashboardComponent {
  get role(): string {
    for (const r of ['Admin', 'Supervisor', 'CareCoordinator', 'Clinician', 'ReadOnly']) {
      if (this.auth.hasRole(r)) return r;
    }
    return '';
  }
  constructor(public auth: AuthService) {}
}
