import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AssignableUser {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly base = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getAssignable(): Observable<AssignableUser[]> {
    return this.http.get<AssignableUser[]>(`${this.base}/assignable`);
  }
}
