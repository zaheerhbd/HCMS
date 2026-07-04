import { Routes } from '@angular/router';

export const FHIR_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'explorer',
    pathMatch: 'full'
  },
  {
    path: 'explorer',
    loadComponent: () =>
      import('./fhir-explorer/fhir-explorer.component').then(m => m.FhirExplorerComponent)
  },
  {
    path: 'import',
    loadComponent: () =>
      import('./fhir-import/fhir-import.component').then(m => m.FhirImportComponent)
  }
];
