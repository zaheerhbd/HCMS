import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { environment } from '../../../../environments/environment';
import { AuditLogDto, AuditLogListDto } from '../../../core/models/notification.models';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatExpansionModule,
    MatProgressBarModule,
    MatChipsModule
  ],
  templateUrl: './audit-log.component.html',
  styleUrls: ['./audit-log.component.scss']
})
export class AuditLogComponent implements OnInit {
  private readonly base = `${environment.apiUrl}/audit`;

  displayedColumns = ['changedAt', 'entityType', 'entityId', 'action', 'changedByUserName', 'expand'];
  logs: AuditLogDto[] = [];
  totalCount = 0;
  page = 0;
  pageSize = 20;
  loading = false;
  expandedRow: AuditLogDto | null = null;

  filterForm: FormGroup;

  entityTypes = ['Patient', 'Case', 'CaseNote', 'User'];
  actions = ['Created', 'Updated', 'Deleted', 'FhirExport'];

  constructor(private http: HttpClient, private fb: FormBuilder) {
    this.filterForm = this.fb.group({
      entityType: [''],
      changedBy: [''],
      action: [''],
      from: [null],
      to: [null]
    });
  }

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading = true;
    const f = this.filterForm.value;
    let params = new HttpParams()
      .set('page', this.page + 1)
      .set('pageSize', this.pageSize);

    if (f.entityType) params = params.set('entityType', f.entityType);
    if (f.changedBy) params = params.set('changedBy', f.changedBy);
    if (f.action) params = params.set('action', f.action);
    if (f.from) params = params.set('from', (f.from as Date).toISOString());
    if (f.to) params = params.set('to', (f.to as Date).toISOString());

    this.http.get<AuditLogListDto>(this.base, { params }).subscribe({
      next: res => {
        this.logs = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  onPageChange(e: PageEvent): void {
    this.page = e.pageIndex;
    this.pageSize = e.pageSize;
    this.loadLogs();
  }

  applyFilter(): void {
    this.page = 0;
    this.loadLogs();
  }

  resetFilter(): void {
    this.filterForm.reset({ entityType: '', changedBy: '', action: '', from: null, to: null });
    this.page = 0;
    this.loadLogs();
  }

  toggleRow(row: AuditLogDto): void {
    this.expandedRow = this.expandedRow === row ? null : row;
  }

  formatJson(json?: string): string {
    if (!json) return '—';
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  }

  actionColor(action: string): string {
    return { Created: 'primary', Updated: 'accent', Deleted: 'warn' }[action] ?? '';
  }
}
