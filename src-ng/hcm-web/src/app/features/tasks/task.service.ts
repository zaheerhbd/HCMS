import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TaskDto } from '../../core/models/case.models';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly base = `${environment.apiUrl}/tasks`;

  constructor(private http: HttpClient) {}

  getMyTasks(): Observable<TaskDto[]> {
    return this.http.get<TaskDto[]>(`${this.base}/my`);
  }
}
