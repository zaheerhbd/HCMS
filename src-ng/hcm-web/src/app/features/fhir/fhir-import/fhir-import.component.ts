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
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { FhirService, BundleEntryResult } from '../../../core/services/fhir.service';

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

const SAMPLE_TRANSACTION_BUNDLE = JSON.stringify({
  resourceType: 'Bundle',
  type: 'transaction',
  entry: [
    {
      fullUrl: 'urn:uuid:patient-1',
      resource: {
        resourceType: 'Patient',
        name: [{ family: 'Rivera', given: ['Maria'] }],
        gender: 'female',
        birthDate: '1978-04-12',
        telecom: [
          { system: 'phone', value: '555-0191' },
          { system: 'email', value: 'maria.rivera@example.com' }
        ]
      },
      request: { method: 'POST', url: 'Patient' }
    },
    {
      fullUrl: 'urn:uuid:encounter-1',
      resource: {
        resourceType: 'Encounter',
        status: 'planned',
        class: { code: 'IMP', system: 'http://terminology.hl7.org/CodeSystem/v3-ActCode' },
        subject: { reference: 'urn:uuid:patient-1' },
        type: [{ text: 'Chronic Disease' }]
      },
      request: { method: 'POST', url: 'Encounter' }
    },
    {
      fullUrl: 'urn:uuid:appointment-1',
      resource: {
        resourceType: 'Appointment',
        status: 'booked',
        serviceType: [{ text: 'Care Coordination' }],
        start: '2026-08-15T09:00:00Z',
        end: '2026-08-15T09:30:00Z',
        participant: [{ actor: { reference: 'urn:uuid:patient-1' }, status: 'accepted' }]
      },
      request: { method: 'POST', url: 'Appointment' }
    },
    {
      fullUrl: 'urn:uuid:task-1',
      resource: {
        resourceType: 'Task',
        status: 'requested',
        intent: 'order',
        description: 'Initial care assessment',
        for: { reference: 'urn:uuid:patient-1' },
        focus: { reference: 'urn:uuid:encounter-1' },
        restriction: { period: { end: '2026-08-20T00:00:00Z' } }
      },
      request: { method: 'POST', url: 'Task' }
    }
  ]
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
    MatSnackBarModule,
    MatTabsModule,
    MatTableModule,
    MatChipsModule,
    MatDividerModule
  ],
  templateUrl: './fhir-import.component.html',
  styleUrls: ['./fhir-import.component.scss']
})
export class FhirImportComponent {
  // ── Single-patient tab ────────────────────────────────────────────────────
  patientForm: FormGroup;
  patientLoading = false;
  patientResult: { success: boolean; mrn?: string; message?: string; errors?: string[] } | null = null;

  // ── Transaction Bundle tab ────────────────────────────────────────────────
  bundleForm: FormGroup;
  bundleLoading = false;
  bundleEntries: BundleEntryResult[] = [];
  bundleErrors: string[] = [];
  bundleSuccess = false;
  bundleColumns = ['resourceType', 'assignedId', 'status'];

  // file upload state
  selectedBundleFile: File | null = null;
  bundleDragOver = false;

  constructor(
    private fb: FormBuilder,
    private fhir: FhirService,
    private router: Router,
    private snack: MatSnackBar
  ) {
    this.patientForm = this.fb.group({ fhirJson: ['', Validators.required] });
    this.bundleForm = this.fb.group({ bundleJson: [''] });
  }

  // ── Patient tab helpers ───────────────────────────────────────────────────

  loadPatientSample(): void {
    this.patientForm.patchValue({ fhirJson: SAMPLE_FHIR_PATIENT });
  }

  onPatientFileChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = e => this.patientForm.patchValue({ fhirJson: e.target?.result as string });
    reader.readAsText(file);
  }

  submitPatient(): void {
    if (this.patientForm.invalid) return;
    this.patientLoading = true;
    this.patientResult = null;

    this.fhir.importPatient(this.patientForm.value.fhirJson).subscribe({
      next: result => {
        this.patientLoading = false;
        this.patientResult = { success: true, ...result };
        this.snack.open(`Patient imported — MRN: ${result.mrn}`, 'View', { duration: 5000 })
          .onAction().subscribe(() => this.router.navigate(['/patients', result.mrn]));
      },
      error: err => {
        this.patientLoading = false;
        const errors = err?.error?.issue?.map((i: { diagnostics: string }) => i.diagnostics) ?? [err?.error?.message ?? 'Import failed.'];
        this.patientResult = { success: false, errors };
      }
    });
  }

  // ── Bundle tab helpers ────────────────────────────────────────────────────

  loadBundleSample(): void {
    this.selectedBundleFile = null;
    this.bundleForm.patchValue({ bundleJson: SAMPLE_TRANSACTION_BUNDLE });
  }

  onBundleFileSelect(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.setSelectedBundleFile(file);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.bundleDragOver = true;
  }

  onDragLeave(): void {
    this.bundleDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.bundleDragOver = false;
    const file = event.dataTransfer?.files?.[0];
    if (file) this.setSelectedBundleFile(file);
  }

  private setSelectedBundleFile(file: File): void {
    this.selectedBundleFile = file;
    this.bundleForm.patchValue({ bundleJson: '' });
  }

  clearBundleFile(): void {
    this.selectedBundleFile = null;
  }

  get canSubmitBundle(): boolean {
    return !!this.selectedBundleFile || !!this.bundleForm.value.bundleJson?.trim();
  }

  submitBundle(): void {
    if (!this.canSubmitBundle) {
      this.snack.open('Provide a .json file or paste bundle JSON below.', 'OK', { duration: 3000 });
      return;
    }

    this.bundleLoading = true;
    this.bundleEntries = [];
    this.bundleErrors = [];
    this.bundleSuccess = false;

    const obs$ = this.selectedBundleFile
      ? this.fhir.uploadBundleFile(this.selectedBundleFile)
      : this.fhir.importTransactionBundle(this.bundleForm.value.bundleJson);

    obs$.subscribe({
      next: result => {
        this.bundleLoading = false;
        this.bundleSuccess = result.success;
        this.bundleEntries = result.entries ?? [];
        this.bundleErrors = result.errors ?? [];
        if (result.success) {
          const patientEntry = result.entries?.find(e => e.resourceType === 'Patient');
          this.snack.open(`Bundle imported — ${result.entries?.length} resources created`, patientEntry ? 'View Patient' : undefined, { duration: 6000 })
            .onAction().subscribe(() => { if (patientEntry) this.router.navigate(['/patients', patientEntry.assignedId]); });
        }
      },
      error: err => {
        this.bundleLoading = false;
        this.bundleSuccess = false;
        const issues: string[] = err?.error?.issue?.map((i: { diagnostics: string }) => i.diagnostics) ?? [];
        this.bundleErrors = issues.length ? issues : [err?.error?.message ?? 'Bundle import failed.'];
      }
    });
  }

  createdPatientMrn(): string | null {
    return this.bundleEntries.find(e => e.resourceType === 'Patient')?.assignedId ?? null;
  }
}
