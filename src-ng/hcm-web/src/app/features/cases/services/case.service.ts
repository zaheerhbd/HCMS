import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CaseDto,
  CaseListItemDto,
  CaseListResult,
  CreateCaseRequest,
  UpdateCaseRequest,
  CaseStatusChangeRequest,
  AddCareTeamMemberRequest,
  CareTeamMemberDto,
  CaseNoteDto,
  CreateCaseNoteRequest
} from '../../../core/models/case.models';

@Injectable({ providedIn: 'root' })
export class CaseService {
  private readonly base = `${environment.apiUrl}/cases`;

  constructor(private http: HttpClient) {}

  list(page = 1, pageSize = 20, status?: string): Observable<CaseListResult> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<CaseListResult>(this.base, { params });
  }

  getByPatient(patientId: string): Observable<CaseListItemDto[]> {
    return this.http.get<CaseListItemDto[]>(`${this.base}/patient/${patientId}`);
  }

  getById(id: string): Observable<CaseDto> {
    return this.http.get<CaseDto>(`${this.base}/${id}`);
  }

  create(request: CreateCaseRequest): Observable<CaseDto> {
    return this.http.post<CaseDto>(this.base, request);
  }

  update(id: string, request: UpdateCaseRequest): Observable<CaseDto> {
    return this.http.put<CaseDto>(`${this.base}/${id}`, request);
  }

  changeStatus(id: string, request: CaseStatusChangeRequest): Observable<CaseDto> {
    return this.http.post<CaseDto>(`${this.base}/${id}/status`, request);
  }

  close(id: string, comment?: string): Observable<CaseDto> {
    return this.http.post<CaseDto>(`${this.base}/${id}/close`, { comment });
  }

  // Care team
  getTeam(caseId: string): Observable<CareTeamMemberDto[]> {
    return this.http.get<CareTeamMemberDto[]>(`${this.base}/${caseId}/team`);
  }

  addTeamMember(caseId: string, request: AddCareTeamMemberRequest): Observable<CareTeamMemberDto> {
    return this.http.post<CareTeamMemberDto>(`${this.base}/${caseId}/team`, request);
  }

  removeTeamMember(caseId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${caseId}/team/${userId}`);
  }

  // Notes
  getNotes(caseId: string): Observable<CaseNoteDto[]> {
    return this.http.get<CaseNoteDto[]>(`${this.base}/${caseId}/notes`);
  }

  addNote(caseId: string, request: CreateCaseNoteRequest): Observable<CaseNoteDto> {
    return this.http.post<CaseNoteDto>(`${this.base}/${caseId}/notes`, request);
  }
}
