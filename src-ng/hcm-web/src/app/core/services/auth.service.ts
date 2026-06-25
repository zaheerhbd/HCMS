import { HttpClient } from '@angular/common/http';
import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { tap, catchError, throwError, Observable, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'hcms_access_token';
  private readonly REFRESH_KEY = 'hcms_refresh_token';
  private readonly USERNAME_KEY = 'hcms_username';
  private readonly ROLES_KEY = 'hcms_roles';

  private _isAuthenticated = new BehaviorSubject<boolean>(this.hasValidToken());
  readonly isAuthenticated$ = this._isAuthenticated.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap(res => this.storeTokens(res)),
      catchError(err => throwError(() => err))
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem(this.REFRESH_KEY);
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, { refreshToken }).pipe(
      tap(res => this.storeTokens(res)),
      catchError(err => {
        this.clearTokens();
        return throwError(() => err);
      })
    );
  }

  logout(): void {
    const refreshToken = localStorage.getItem(this.REFRESH_KEY);
    if (refreshToken) {
      this.http.post(`${environment.apiUrl}/auth/logout`, { refreshToken }).subscribe();
    }
    this.clearTokens();
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getUsername(): string | null {
    return localStorage.getItem(this.USERNAME_KEY);
  }

  getRoles(): string[] {
    const raw = localStorage.getItem(this.ROLES_KEY);
    return raw ? JSON.parse(raw) : [];
  }

  hasRole(role: string): boolean {
    return this.getRoles().includes(role);
  }

  private hasValidToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }

  private storeTokens(res: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, res.accessToken);
    localStorage.setItem(this.REFRESH_KEY, res.refreshToken);
    localStorage.setItem(this.USERNAME_KEY, res.username);
    localStorage.setItem(this.ROLES_KEY, JSON.stringify(res.roles));
    this._isAuthenticated.next(true);
  }

  private clearTokens(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USERNAME_KEY);
    localStorage.removeItem(this.ROLES_KEY);
    this._isAuthenticated.next(false);
  }
}
