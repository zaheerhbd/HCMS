import { Routes } from '@angular/router';

export const CASES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./cases.component').then(m => m.CasesComponent)
  }
];
