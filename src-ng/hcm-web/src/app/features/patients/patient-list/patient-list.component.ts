import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { PatientService } from '../services/patient.service';
import { PatientListItemDto } from '../../../core/models/patient.models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-patient-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatSnackBarModule
  ],
  templateUrl: './patient-list.component.html',
  styleUrls: ['./patient-list.component.scss']
})
export class PatientListComponent implements OnInit, OnDestroy {
  rows: PatientListItemDto[] = [];
  totalCount = 0;
  pageSize = 20;
  page = 1;
  loading = false;
  searchControl = new FormControl('');
  columns = ['mrn', 'fullName', 'dateOfBirth', 'phone', 'actions'];
  private destroy$ = new Subject<void>();

  get canCreate(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator');
  }

  constructor(
    private svc: PatientService,
    private router: Router,
    private snack: MatSnackBar,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.page = 1;
      this.load();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    this.loading = true;
    this.svc.search(this.searchControl.value ?? '', this.page, this.pageSize).subscribe({
      next: res => {
        this.rows = res.results;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: () => {
        this.snack.open('Failed to load patients.', 'Close', { duration: 4000 });
        this.loading = false;
      }
    });
  }

  onPage(e: PageEvent): void {
    this.page = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.load();
  }

  navigate(mrn: string): void {
    this.router.navigate(['/patients', mrn]);
  }
}
