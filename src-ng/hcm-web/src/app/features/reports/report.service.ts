import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CasesByStatusRow { status: string; count: number; percentage: number; }
export interface CasesByStatusReport { rows: CasesByStatusRow[]; totalCases: number; generatedAt: string; }

export interface CaseDurationRow { caseType: string; caseCount: number; averageDays: number; minDays: number; maxDays: number; }
export interface CaseDurationReport { rows: CaseDurationRow[]; generatedAt: string; }

export interface TasksByAssigneeRow { userId: string; userName: string; totalTasks: number; doneTasks: number; overdueTasks: number; openTasks: number; inProgressTasks: number; }
export interface TasksByAssigneeReport { rows: TasksByAssigneeRow[]; generatedAt: string; }

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly base = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  getCasesByStatus(): Observable<CasesByStatusReport> {
    return this.http.get<CasesByStatusReport>(`${this.base}/cases-by-status`);
  }

  getCaseDuration(): Observable<CaseDurationReport> {
    return this.http.get<CaseDurationReport>(`${this.base}/case-duration`);
  }

  getTasksByAssignee(): Observable<TasksByAssigneeReport> {
    return this.http.get<TasksByAssigneeReport>(`${this.base}/tasks-by-assignee`);
  }

  downloadCsv(reportType: 'cases-by-status' | 'case-duration' | 'tasks-by-assignee'): void {
    const url = `${this.base}/${reportType}?format=csv`;
    const a = document.createElement('a');
    a.href = url;
    a.download = `${reportType}.csv`;
    // The browser will handle auth via cookie/header — attach token manually if needed
    this.http.get(url, { responseType: 'blob' }).subscribe(blob => {
      const objectUrl = URL.createObjectURL(blob);
      a.href = objectUrl;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(objectUrl);
    });
  }
}
