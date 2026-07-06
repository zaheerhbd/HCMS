import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { jwtInterceptor } from './jwt.interceptor';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../models/auth.models';

describe('jwtInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([jwtInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: jasmine.createSpyObj('Router', ['navigate']) }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should attach an Authorization header when a token is present', () => {
    spyOn(authService, 'getAccessToken').and.returnValue('valid.jwt.token');

    http.get('/api/patients').subscribe();
    const req = httpMock.expectOne('/api/patients');
    expect(req.request.headers.get('Authorization')).toBe('Bearer valid.jwt.token');
    req.flush({});
  });

  it('should not attach an Authorization header when no token is present', () => {
    spyOn(authService, 'getAccessToken').and.returnValue(null);

    http.get('/api/patients').subscribe();
    const req = httpMock.expectOne('/api/patients');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('should refresh the token and retry the original request on a 401', () => {
    spyOn(authService, 'getAccessToken').and.returnValue('expired.token');
    const refreshResponse: AuthResponse = {
      accessToken: 'new.token',
      refreshToken: 'new.refresh',
      expiresAt: new Date().toISOString(),
      username: 'jdoe',
      roles: []
    };
    spyOn(authService, 'refreshToken').and.returnValue(of(refreshResponse));

    let result: unknown;
    http.get('/api/patients').subscribe(res => (result = res));

    const firstReq = httpMock.expectOne('/api/patients');
    firstReq.flush({ message: 'expired' }, { status: 401, statusText: 'Unauthorized' });

    const retriedReq = httpMock.expectOne('/api/patients');
    expect(retriedReq.request.headers.get('Authorization')).toBe('Bearer new.token');
    retriedReq.flush({ ok: true });

    expect(result).toEqual({ ok: true });
    expect(authService.refreshToken).toHaveBeenCalled();
  });

  it('should not attempt a refresh loop when the 401 comes from an /auth/ endpoint', () => {
    spyOn(authService, 'getAccessToken').and.returnValue('expired.token');
    const refreshSpy = spyOn(authService, 'refreshToken');

    let caught: HttpErrorResponse | undefined;
    http.get(`${environment.apiUrl}/auth/refresh`).subscribe({
      error: (e) => (caught = e)
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
    req.flush({ message: 'expired' }, { status: 401, statusText: 'Unauthorized' });

    expect(refreshSpy).not.toHaveBeenCalled();
    expect(caught?.status).toBe(401);
  });

  it('should log the user out when the refresh call itself fails', () => {
    spyOn(authService, 'getAccessToken').and.returnValue('expired.token');
    const logoutSpy = spyOn(authService, 'logout');
    spyOn(authService, 'refreshToken').and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 401 }))
    );

    let caught: unknown;
    http.get('/api/patients').subscribe({ error: e => (caught = e) });

    httpMock.expectOne('/api/patients').flush(
      { message: 'expired' },
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(logoutSpy).toHaveBeenCalled();
    expect(caught).toBeTruthy();
  });
});
