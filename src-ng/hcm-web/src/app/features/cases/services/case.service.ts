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

  getByPatientMrn(mrn: string): Observable<CaseListItemDto[]> {
    return this.http.get<CaseListItemDto[]>(`${this.base}/patient/mrn/${mrn}`);
  }

  getById(caseNumber: string): Observable<CaseDto> {
    return this.http.get<CaseDto>(`${this.base}/${caseNumber}`);
  }

  create(request: CreateCaseRequest): Observable<CaseDto> {
    const params = new HttpParams().set('patientMrn', request.patientMrn);
    const body = { caseTypeId: request.caseTypeId, notes: request.notes };
    return this.http.post<CaseDto>(this.base, body, { params });
  }

  update(caseNumber: string, request: UpdateCaseRequest): Observable<CaseDto> {
    return this.http.put<CaseDto>(`${this.base}/${caseNumber}`, request);
  }

  changeStatus(caseNumber: string, request: CaseStatusChangeRequest): Observable<CaseDto> {
    return this.http.post<CaseDto>(`${this.base}/${caseNumber}/status`, request);
  }

  close(caseNumber: string, comment?: string): Observable<CaseDto> {
    return this.http.post<CaseDto>(`${this.base}/${caseNumber}/close`, { comment });
  }

  // Care team
  getTeam(caseNumber: string): Observable<CareTeamMemberDto[]> {
    return this.http.get<CareTeamMemberDto[]>(`${this.base}/${caseNumber}/team`);
  }

  addTeamMember(caseNumber: string, request: AddCareTeamMemberRequest): Observable<CareTeamMemberDto> {
    return this.http.post<CareTeamMemberDto>(`${this.base}/${caseNumber}/team`, request);
  }

  removeTeamMember(caseNumber: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${caseNumber}/team/${userId}`);
  }

  // Notes
  getNotes(caseNumber: string): Observable<CaseNoteDto[]> {
    return this.http.get<CaseNoteDto[]>(`${this.base}/${caseNumber}/notes`);
  }

  addNote(caseNumber: string, request: CreateCaseNoteRequest): Observable<CaseNoteDto> {
    return this.http.post<CaseNoteDto>(`${this.base}/${caseNumber}/notes`, request);
  }
}
