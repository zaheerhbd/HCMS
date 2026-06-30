import { Routes } from '@angular/router';

export const CASES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./case-list/case-list.component').then(m => m.CaseListComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./case-form/case-form.component').then(m => m.CaseFormComponent)
  },
  {
    path: ':caseNumber',
    loadComponent: () =>
      import('./case-detail/case-detail.component').then(m => m.CaseDetailComponent)
  }
];
