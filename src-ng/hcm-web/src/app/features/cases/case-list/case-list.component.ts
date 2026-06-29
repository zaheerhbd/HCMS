import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CaseService } from '../services/case.service';
import { CaseListItemDto } from '../../../core/models/case.models';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge.component';
import { AuthService } from '../../../core/services/auth.service';

const STATUSES = ['Open', 'InProgress', 'OnHold', 'Closed', 'Reopened'];

@Component({
  selector: 'app-case-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    StatusBadgeComponent
  ],
  template: `
    <div class="page-header">
      <h1>Cases</h1>
      <button mat-flat-button color="primary" routerLink="new" *ngIf="canCreate">
        <mat-icon>create_new_folder</mat-icon> New Case
      </button>
    </div>

    <div class="filters">
      <mat-form-field appearance="outline">
        <mat-label>Filter by status</mat-label>
        <mat-select [formControl]="statusFilter">
          <mat-option value="">All</mat-option>
          <mat-option *ngFor="let s of statuses" [value]="s">{{ s }}</mat-option>
        </mat-select>
      </mat-form-field>
    </div>

    <div *ngIf="loading" class="spinner-wrap"><mat-spinner diameter="40"></mat-spinner></div>

    <table mat-table [dataSource]="rows" *ngIf="!loading">
      <ng-container matColumnDef="caseNumber">
        <th mat-header-cell *matHeaderCellDef>Case #</th>
        <td mat-cell *matCellDef="let c">{{ c.caseNumber }}</td>
      </ng-container>
      <ng-container matColumnDef="caseTypeName">
        <th mat-header-cell *matHeaderCellDef>Type</th>
        <td mat-cell *matCellDef="let c">{{ c.caseTypeName }}</td>
      </ng-container>
      <ng-container matColumnDef="currentStatus">
        <th mat-header-cell *matHeaderCellDef>Status</th>
        <td mat-cell *matCellDef="let c">
          <app-status-badge [status]="c.currentStatus"></app-status-badge>
        </td>
      </ng-container>
      <ng-container matColumnDef="assignedToUserName">
        <th mat-header-cell *matHeaderCellDef>Assigned To</th>
        <td mat-cell *matCellDef="let c">{{ c.assignedToUserName ?? '—' }}</td>
      </ng-container>
      <ng-container matColumnDef="openedAt">
        <th mat-header-cell *matHeaderCellDef>Opened</th>
        <td mat-cell *matCellDef="let c">{{ c.openedAt | date:'mediumDate' }}</td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="columns"></tr>
      <tr mat-row *matRowDef="let row; columns: columns;" class="clickable-row"
        (click)="navigate(row.id)"></tr>
    </table>

    <div *ngIf="!loading && rows.length === 0" class="empty-state">No cases found.</div>

    <mat-paginator
      [length]="totalCount"
      [pageSize]="pageSize"
      [pageSizeOptions]="[10, 20, 50]"
      (page)="onPage($event)">
    </mat-paginator>
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
    .filters { margin-bottom: 12px; }
    .spinner-wrap { display: flex; justify-content: center; padding: 40px; }
    .empty-state { text-align: center; padding: 40px; color: #757575; }
    .clickable-row { cursor: pointer; }
    .clickable-row:hover { background: #f5f5f5; }
    table { width: 100%; }
  `]
})
export class CaseListComponent implements OnInit {
  rows: CaseListItemDto[] = [];
  totalCount = 0;
  pageSize = 20;
  page = 1;
  loading = false;
  statuses = STATUSES;
  statusFilter = new FormControl('');
  columns = ['caseNumber', 'caseTypeName', 'currentStatus', 'assignedToUserName', 'openedAt'];

  get canCreate(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator');
  }

  constructor(
    private svc: CaseService,
    private router: Router,
    private snack: MatSnackBar,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.load();
    this.statusFilter.valueChanges.subscribe(() => {
      this.page = 1;
      this.load();
    });
  }

  load(): void {
    this.loading = true;
    this.svc.list(this.page, this.pageSize, this.statusFilter.value ?? undefined).subscribe({
      next: res => {
        this.rows = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: () => {
        this.snack.open('Failed to load cases.', 'Close', { duration: 4000 });
        this.loading = false;
      }
    });
  }

  onPage(e: PageEvent): void {
    this.page = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.load();
  }

  navigate(id: string): void {
    this.router.navigate(['/cases', id]);
  }
}
