import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CaseStatusCount { status: string; count: number; }
export interface OverdueTaskSummary { id: string; title: string; caseNumber: string; priority: string; dueDate: string; }
export interface TodayAppointment { id: string; patientName: string; mrn: string; scheduledStart: string; appointmentType: string; status: string; }

export interface CoordinatorDashboard {
  openCasesCount: number;
  overdueTasksCount: number;
  todayAppointmentsCount: number;
  inactiveCasesCount: number;
  casesByStatus: CaseStatusCount[];
  overdueTasks: OverdueTaskSummary[];
  todayAppointments: TodayAppointment[];
}

export interface UserCaseload { userId: string; userName: string; openCases: number; overdueTasks: number; }
export interface EscalatedCase { caseNumber: string; patientName: string; assignedTo: string; escalatedAt: string; }
export interface InactiveCase { caseNumber: string; patientName: string; assignedTo: string; lastActivityAt: string; daysSinceActivity: number; }

export interface SupervisorDashboard {
  totalActiveCases: number;
  escalatedCasesCount: number;
  inactiveCasesCount: number;
  teamCaseloads: UserCaseload[];
  escalatedCases: EscalatedCase[];
  inactiveCases: InactiveCase[];
}

export interface DailyLoginCount { date: string; count: number; }
export interface RoleUserCount { role: string; count: number; }

export interface AdminDashboard {
  totalActiveUsers: number;
  totalPatients: number;
  totalCases: number;
  openCases: number;
  loginsLast7Days: number;
  fhirImportsLast30Days: number;
  loginTrend: DailyLoginCount[];
  usersByRole: RoleUserCount[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly base = `${environment.apiUrl}/dashboard`;

  constructor(private http: HttpClient) {}

  getCoordinator(): Observable<CoordinatorDashboard> {
    return this.http.get<CoordinatorDashboard>(`${this.base}/coordinator`);
  }

  getSupervisor(): Observable<SupervisorDashboard> {
    return this.http.get<SupervisorDashboard>(`${this.base}/supervisor`);
  }

  getAdmin(): Observable<AdminDashboard> {
    return this.http.get<AdminDashboard>(`${this.base}/admin`);
  }
}
