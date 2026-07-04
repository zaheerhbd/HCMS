import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { FhirService } from '../../../core/services/fhir.service';

const SAMPLE_FHIR_PATIENT = JSON.stringify({
  resourceType: 'Patient',
  name: [{ use: 'official', family: 'Smith', given: ['Jane'] }],
  birthDate: '1985-06-15',
  gender: 'female',
  telecom: [
    { system: 'phone', value: '555-0100', use: 'home' },
    { system: 'email', value: 'jane.smith@example.com' }
  ],
  address: [{ use: 'home', line: ['123 Main St'], city: 'Springfield', state: 'IL', postalCode: '62701' }]
}, null, 2);

@Component({
  selector: 'app-fhir-import',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './fhir-import.component.html',
  styleUrls: ['./fhir-import.component.scss']
})
export class FhirImportComponent {
  form: FormGroup;
  loading = false;
  importResult: { success: boolean; mrn?: string; message?: string; errors?: string[] } | null = null;

  constructor(
    private fb: FormBuilder,
    private fhir: FhirService,
    private router: Router,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      fhirJson: ['', Validators.required]
    });
  }

  loadSample(): void {
    this.form.patchValue({ fhirJson: SAMPLE_FHIR_PATIENT });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = e => this.form.patchValue({ fhirJson: e.target?.result as string });
    reader.readAsText(file);
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.importResult = null;

    this.fhir.importPatient(this.form.value.fhirJson).subscribe({
      next: result => {
        this.loading = false;
        this.importResult = { success: true, ...result };
        this.snack.open(`Patient imported — MRN: ${result.mrn}`, 'View', { duration: 5000 })
          .onAction().subscribe(() => this.router.navigate(['/patients', result.mrn]));
      },
      error: err => {
        this.loading = false;
        const errors = err?.error?.issue?.map((i: { diagnostics: string }) => i.diagnostics) ?? [err?.error?.message ?? 'Import failed.'];
        this.importResult = { success: false, errors };
      }
    });
  }
}
