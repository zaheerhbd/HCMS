import { Routes } from '@angular/router';

export const PATIENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./patient-list/patient-list.component').then(m => m.PatientListComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./patient-form/patient-form.component').then(m => m.PatientFormComponent)
  },
  {
    path: ':mrn',
    loadComponent: () =>
      import('./patient-detail/patient-detail.component').then(m => m.PatientDetailComponent)
  },
  {
    path: ':mrn/edit',
    loadComponent: () =>
      import('./patient-form/patient-form.component').then(m => m.PatientFormComponent)
  }
];
