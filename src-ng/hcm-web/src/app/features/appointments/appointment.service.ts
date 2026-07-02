import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AppointmentDto,
  AppointmentListResult,
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
  AppointmentStatusChangeRequest,
  ProviderDto
} from '../../core/models/appointment.models';

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private readonly base = `${environment.apiUrl}/appointments`;

  constructor(private http: HttpClient) {}

  list(page = 1, pageSize = 20, status?: string, patientMrn?: string): Observable<AppointmentListResult> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (patientMrn) params = params.set('patientMrn', patientMrn);
    return this.http.get<AppointmentListResult>(this.base, { params });
  }

  getById(id: string): Observable<AppointmentDto> {
    return this.http.get<AppointmentDto>(`${this.base}/${id}`);
  }

  getByPatientMrn(mrn: string): Observable<AppointmentDto[]> {
    return this.http.get<AppointmentDto[]>(`${this.base}/patient/${mrn}`);
  }

  create(request: CreateAppointmentRequest): Observable<AppointmentDto> {
    return this.http.post<AppointmentDto>(this.base, request);
  }

  update(id: string, request: UpdateAppointmentRequest): Observable<AppointmentDto> {
    return this.http.put<AppointmentDto>(`${this.base}/${id}`, request);
  }

  changeStatus(id: string, request: AppointmentStatusChangeRequest): Observable<AppointmentDto> {
    return this.http.post<AppointmentDto>(`${this.base}/${id}/status`, request);
  }

  getProviders(): Observable<ProviderDto[]> {
    return this.http.get<ProviderDto[]>(`${this.base}/providers`);
  }
}
