import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    loadComponent: () =>
      import('./shell/app-shell/app-shell.component').then(m => m.AppShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'patients',
        loadChildren: () =>
          import('./features/patients/patients.routes').then(m => m.PATIENTS_ROUTES)
      },
      {
        path: 'cases',
        loadChildren: () =>
          import('./features/cases/cases.routes').then(m => m.CASES_ROUTES)
      },
      {
        path: 'tasks',
        loadComponent: () =>
          import('./features/tasks/tasks.component').then(m => m.TasksComponent)
      },
      {
        path: 'documents',
        loadComponent: () =>
          import('./features/documents/documents.component').then(m => m.DocumentsComponent)
      },
      {
        path: 'appointments',
        loadComponent: () =>
          import('./features/appointments/appointments.component').then(m => m.AppointmentsComponent)
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/reports.component').then(m => m.ReportsComponent)
      },
      {
        path: 'admin',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/admin/admin.component').then(m => m.AdminComponent)
      }
    ]
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./features/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  { path: '**', redirectTo: '' }
];
