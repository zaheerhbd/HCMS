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
        path: 'appointments',
        loadChildren: () =>
          import('./features/appointments/appointments.routes').then(m => m.APPOINTMENTS_ROUTES)
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/reports.component').then(m => m.ReportsComponent)
      },
      {
        path: 'fhir',
        loadChildren: () =>
          import('./features/fhir/fhir.routes').then(m => m.FHIR_ROUTES)
      },
      {
        path: 'admin',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadChildren: () =>
          import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
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
