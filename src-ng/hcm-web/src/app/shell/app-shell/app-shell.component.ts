import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { AuthService } from '../../core/services/auth.service';

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
    MatBadgeModule
  ],
  templateUrl: './app-shell.component.html',
  styleUrls: ['./app-shell.component.scss']
})
export class AppShellComponent {
  navItems: NavItem[] = [
    { label: 'Dashboard',    icon: 'dashboard',        route: '/dashboard' },
    { label: 'Patients',     icon: 'people',           route: '/patients' },
    { label: 'Cases',        icon: 'folder_open',      route: '/cases' },
    { label: 'Tasks',        icon: 'task_alt',         route: '/tasks' },
    { label: 'Documents',    icon: 'description',      route: '/documents' },
    { label: 'Appointments', icon: 'event',            route: '/appointments' },
    { label: 'Reports',      icon: 'bar_chart',        route: '/reports' },
    { label: 'Admin',        icon: 'admin_panel_settings', route: '/admin', roles: ['Admin'] }
  ];

  constructor(public auth: AuthService) {}

  get visibleNav(): NavItem[] {
    return this.navItems.filter(item =>
      !item.roles || item.roles.some(r => this.auth.hasRole(r))
    );
  }
}
