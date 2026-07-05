import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FhirImportResult {
  mrn?: string;
  message?: string;
  errors?: string[];
}

export interface BundleEntryResult {
  resourceType: string;
  assignedId: string;
  status: string;
}

export interface FhirBundleImportResult {
  success: boolean;
  entries: BundleEntryResult[];
  errors: string[];
}

@Injectable({ providedIn: 'root' })
export class FhirService {
  private readonly base = `${environment.apiUrl.replace('/api', '')}/fhir/R4`;
  private readonly fhirHeaders = new HttpHeaders({ 'Content-Type': 'application/fhir+json' });

  constructor(private http: HttpClient) {}

  getMetadata(): Observable<object> {
    return this.http.get<object>(`${this.base}/metadata`);
  }

  getResource(type: string, id: string): Observable<object> {
    return this.http.get<object>(`${this.base}/${type}/${id}`);
  }

  searchPatients(params: { _id?: string; name?: string; birthdate?: string; identifier?: string }): Observable<object> {
    return this.http.get<object>(`${this.base}/Patient`, { params: params as Record<string, string> });
  }

  getEverything(mrn: string): Observable<Blob> {
    return this.http.get(`${this.base}/Patient/${mrn}/$everything`, { responseType: 'blob' });
  }

  importPatient(fhirJson: string): Observable<FhirImportResult> {
    return this.http.post<FhirImportResult>(`${this.base}/Patient`, fhirJson, { headers: this.fhirHeaders });
  }

  importTransactionBundle(bundleJson: string): Observable<FhirBundleImportResult> {
    return this.http.post<FhirBundleImportResult>(`${this.base}`, bundleJson, { headers: this.fhirHeaders });
  }

  uploadBundleFile(file: File): Observable<FhirBundleImportResult> {
    const formData = new FormData();
    formData.append('bundleFile', file);
    return this.http.post<FhirBundleImportResult>(`${this.base}/$import`, formData);
  }
}
