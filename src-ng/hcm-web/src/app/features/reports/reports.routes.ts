import { Routes } from '@angular/router';
import { ReportsComponent } from './reports.component';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    component: ReportsComponent,
    children: [
      { path: '', redirectTo: 'cases-by-status', pathMatch: 'full' },
      {
        path: 'cases-by-status',
        loadComponent: () =>
          import('./cases-by-status/cases-by-status.component').then(m => m.CasesByStatusComponent)
      },
      {
        path: 'case-duration',
        loadComponent: () =>
          import('./case-duration/case-duration.component').then(m => m.CaseDurationComponent)
      },
      {
        path: 'tasks-by-assignee',
        loadComponent: () =>
          import('./tasks-by-assignee/tasks-by-assignee.component').then(m => m.TasksByAssigneeComponent)
      }
    ]
  }
];
