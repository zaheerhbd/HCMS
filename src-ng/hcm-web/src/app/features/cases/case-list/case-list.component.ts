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
  templateUrl: './case-list.component.html',
  styleUrls: ['./case-list.component.scss']
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

  navigate(caseNumber: string): void {
    this.router.navigate(['/cases', caseNumber]);
  }
}
