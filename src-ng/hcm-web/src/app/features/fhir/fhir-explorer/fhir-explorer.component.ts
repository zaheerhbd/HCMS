import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { NgxJsonViewerModule } from 'ngx-json-viewer';
import { FhirService } from '../../../core/services/fhir.service';

const RESOURCE_TYPES = ['Patient', 'Encounter', 'Appointment', 'Task', 'DocumentReference', 'metadata'];

@Component({
  selector: 'app-fhir-explorer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSnackBarModule,
    NgxJsonViewerModule
  ],
  templateUrl: './fhir-explorer.component.html',
  styleUrls: ['./fhir-explorer.component.scss']
})
export class FhirExplorerComponent {
  form: FormGroup;
  result: object | null = null;
  loading = false;
  resourceTypes = RESOURCE_TYPES;

  constructor(private fb: FormBuilder, private fhir: FhirService, private snack: MatSnackBar) {
    this.form = this.fb.group({
      resourceType: ['Patient', Validators.required],
      id: ['']
    });
  }

  fetch(): void {
    const { resourceType, id } = this.form.value;
    this.loading = true;
    this.result = null;

    const call$ = resourceType === 'metadata'
      ? this.fhir.getMetadata()
      : this.fhir.getResource(resourceType, id?.trim() || '');

    call$.subscribe({
      next: data => { this.result = data; this.loading = false; },
      error: err => {
        this.snack.open(err?.error?.issue?.[0]?.diagnostics ?? 'Request failed.', 'Close', { duration: 4000 });
        this.loading = false;
      }
    });
  }
}
