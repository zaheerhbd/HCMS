import { Component, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationBellComponent } from '../../core/components/notification-bell/notification-bell.component';
import { TaskService } from '../../features/tasks/task.service';
import { TaskDto } from '../../core/models/case.models';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  roles?: string[];
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
    NotificationBellComponent
  ],
  templateUrl: './app-shell.component.html',
  styleUrls: ['./app-shell.component.scss']
})
export class AppShellComponent implements OnInit {
  overdueTaskCount = 0;

  navItems: NavItem[] = [
    { label: 'Dashboard',    icon: 'dashboard',            route: '/dashboard' },
    { label: 'Patients',     icon: 'people',               route: '/patients' },
    { label: 'Cases',        icon: 'folder_open',          route: '/cases' },
    { label: 'Tasks',        icon: 'task_alt',             route: '/tasks' },
    { label: 'Appointments', icon: 'event',                route: '/appointments' },
    { label: 'Reports',      icon: 'bar_chart',            route: '/reports' },
    { label: 'FHIR',         icon: 'hub',                  route: '/fhir' },
    { label: 'Admin',        icon: 'admin_panel_settings', route: '/admin', roles: ['Admin'] }
  ];

  constructor(
    public auth: AuthService,
    private taskSvc: TaskService,
    private notificationSvc: NotificationService
  ) {}

  ngOnInit(): void {
    this.notificationSvc.startPolling();

    this.taskSvc.getMyTasks().subscribe({
      next: (tasks: TaskDto[]) => {
        const now = new Date();
        this.overdueTaskCount = tasks.filter(t =>
          t.dueDate &&
          t.status !== 'Done' &&
          t.status !== 'Cancelled' &&
          new Date(t.dueDate) < now
        ).length;
      },
      error: () => {}
    });
  }

  get visibleNav(): NavItem[] {
    return this.navItems.filter(item =>
      !item.roles || item.roles.some(r => this.auth.hasRole(r))
    );
  }
}
