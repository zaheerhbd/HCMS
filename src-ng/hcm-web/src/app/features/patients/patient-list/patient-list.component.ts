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
  template: `
    <div class="page-header">
      <h1>Patients</h1>
      <button mat-flat-button color="primary" routerLink="new"
        *ngIf="canCreate">
        <mat-icon>person_add</mat-icon> New Patient
      </button>
    </div>

    <mat-form-field appearance="outline" class="search-field">
      <mat-label>Search by name, MRN, or insurance ID</mat-label>
      <input matInput [formControl]="searchControl" />
      <mat-icon matSuffix>search</mat-icon>
    </mat-form-field>

    <div *ngIf="loading" class="spinner-wrap">
      <mat-spinner diameter="40"></mat-spinner>
    </div>

    <table mat-table [dataSource]="rows" *ngIf="!loading">
      <ng-container matColumnDef="mrn">
        <th mat-header-cell *matHeaderCellDef>MRN</th>
        <td mat-cell *matCellDef="let p">{{ p.mrn }}</td>
      </ng-container>
      <ng-container matColumnDef="fullName">
        <th mat-header-cell *matHeaderCellDef>Name</th>
        <td mat-cell *matCellDef="let p">{{ p.fullName }}</td>
      </ng-container>
      <ng-container matColumnDef="dateOfBirth">
        <th mat-header-cell *matHeaderCellDef>Date of Birth</th>
        <td mat-cell *matCellDef="let p">{{ p.dateOfBirth | date:'mediumDate' }}</td>
      </ng-container>
      <ng-container matColumnDef="phone">
        <th mat-header-cell *matHeaderCellDef>Phone</th>
        <td mat-cell *matCellDef="let p">{{ p.phone ?? '—' }}</td>
      </ng-container>
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let p">
          <button mat-icon-button [routerLink]="p.mrn" matTooltip="View patient">
            <mat-icon>visibility</mat-icon>
          </button>
          <button mat-icon-button [routerLink]="[p.mrn, 'edit']" matTooltip="Edit patient"
            *ngIf="canCreate">
            <mat-icon>edit</mat-icon>
          </button>
        </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="columns"></tr>
      <tr mat-row *matRowDef="let row; columns: columns;" class="clickable-row"
        (click)="navigate(row.mrn)"></tr>
    </table>

    <div *ngIf="!loading && rows.length === 0" class="empty-state">
      No patients found.
    </div>

    <mat-paginator
      [length]="totalCount"
      [pageSize]="pageSize"
      [pageSizeOptions]="[10, 20, 50]"
      (page)="onPage($event)">
    </mat-paginator>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
    .search-field { width: 100%; margin-bottom: 16px; }
    .spinner-wrap { display: flex; justify-content: center; padding: 40px; }
    .empty-state { text-align: center; padding: 40px; color: #757575; }
    .clickable-row { cursor: pointer; }
    .clickable-row:hover { background: #f5f5f5; }
    table { width: 100%; }
  `]
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
