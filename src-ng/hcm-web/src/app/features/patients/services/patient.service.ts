import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  PatientDto,
  PatientSearchResult,
  CreatePatientRequest,
  UpdatePatientRequest
} from '../../../core/models/patient.models';

@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly base = `${environment.apiUrl}/patients`;

  constructor(private http: HttpClient) {}

  search(search: string = '', page = 1, pageSize = 20): Observable<PatientSearchResult> {
    const params = new HttpParams()
      .set('search', search)
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PatientSearchResult>(`${this.base}/search`, { params });
  }

  getById(id: string): Observable<PatientDto> {
    return this.http.get<PatientDto>(`${this.base}/${id}`);
  }

  create(request: CreatePatientRequest): Observable<PatientDto> {
    return this.http.post<PatientDto>(this.base, request);
  }

  update(id: string, request: UpdatePatientRequest): Observable<PatientDto> {
    return this.http.put<PatientDto>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
