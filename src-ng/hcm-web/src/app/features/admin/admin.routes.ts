import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./admin.component').then(m => m.AdminComponent)
  },
  {
    path: 'audit',
    loadComponent: () =>
      import('./audit-log/audit-log.component').then(m => m.AuditLogComponent)
  }
];
