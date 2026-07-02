import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CaseService } from '../services/case.service';
import { CaseTypeDto } from '../../../core/models/case.models';

// Seed CaseTypes are fixed by the seeder — hardcoded here to avoid an extra API call
const DEFAULT_CASE_TYPES: CaseTypeDto[] = [
  { id: 1, name: 'Chronic Disease', isActive: true },
  { id: 2, name: 'Post-Surgery', isActive: true },
  { id: 3, name: 'Mental Health', isActive: true },
  { id: 4, name: 'Preventive', isActive: true },
  { id: 5, name: 'Behavioral', isActive: true }
];

@Component({
  selector: 'app-case-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './case-form.component.html',
  styleUrls: ['./case-form.component.scss']
})
export class CaseFormComponent implements OnInit {
  form: FormGroup;
  caseTypes = DEFAULT_CASE_TYPES;
  saving = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private svc: CaseService,
    private snack: MatSnackBar
  ) {
    this.form = this.fb.group({
      patientMrn: ['', Validators.required],
      caseTypeId: ['', Validators.required],
      notes: ['']
    });
  }

  ngOnInit(): void {
    // Pre-fill patientMrn if navigated from patient detail page
    const patientMrn = this.route.snapshot.queryParamMap.get('patientMrn');
    if (patientMrn) this.form.patchValue({ patientMrn });
  }

  submit(): void {
    if (this.form.invalid || this.saving) return;
    this.saving = true;
    this.svc.create(this.form.value).subscribe({
      next: c => {
        this.snack.open('Case created.', 'Close', { duration: 3000 });
        this.router.navigate(['/cases', c.caseNumber]);
      },
      error: err => {
        const msg = err.error?.error ?? err.error?.errors?.[0] ?? 'Failed to create case.';
        this.snack.open(msg, 'Close', { duration: 5000 });
        this.saving = false;
      }
    });
  }
}
