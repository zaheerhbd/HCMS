import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { AuthResponse } from '../models/auth.models';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockResponse: AuthResponse = {
    accessToken: 'access.token.value',
    refreshToken: 'refresh.token.value',
    expiresAt: new Date(Date.now() + 3600_000).toISOString(),
    username: 'jdoe',
    roles: ['CareCoordinator']
  };

  beforeEach(() => {
    localStorage.clear();
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('login() should store tokens, username, and roles in localStorage', () => {
    service.login({ username: 'jdoe', password: 'secret' }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);

    expect(localStorage.getItem('hcms_access_token')).toBe(mockResponse.accessToken);
    expect(localStorage.getItem('hcms_refresh_token')).toBe(mockResponse.refreshToken);
    expect(localStorage.getItem('hcms_username')).toBe(mockResponse.username);
    expect(JSON.parse(localStorage.getItem('hcms_roles')!)).toEqual(mockResponse.roles);
  });

  it('login() should emit true on isAuthenticated$ after a successful login', () => {
    const emitted: boolean[] = [];
    service.isAuthenticated$.subscribe(v => emitted.push(v));

    service.login({ username: 'jdoe', password: 'secret' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(mockResponse);

    expect(emitted).toContain(true);
  });

  it('logout() should clear all localStorage keys and navigate to /login', () => {
    localStorage.setItem('hcms_access_token', 'x');
    localStorage.setItem('hcms_refresh_token', 'y');
    localStorage.setItem('hcms_username', 'jdoe');
    localStorage.setItem('hcms_roles', JSON.stringify(['Admin']));

    service.logout();
    httpMock.expectOne(`${environment.apiUrl}/auth/logout`).flush({});

    expect(localStorage.getItem('hcms_access_token')).toBeNull();
    expect(localStorage.getItem('hcms_refresh_token')).toBeNull();
    expect(localStorage.getItem('hcms_username')).toBeNull();
    expect(localStorage.getItem('hcms_roles')).toBeNull();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('logout() should not call the API when no refresh token is present', () => {
    service.logout();
    httpMock.expectNone(`${environment.apiUrl}/auth/logout`);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('getRoles() should return [] when no roles are stored', () => {
    expect(service.getRoles()).toEqual([]);
  });

  it('hasRole() should reflect stored roles after login', () => {
    service.login({ username: 'jdoe', password: 'secret' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(mockResponse);

    expect(service.hasRole('CareCoordinator')).toBeTrue();
    expect(service.hasRole('Admin')).toBeFalse();
  });

  it('refreshToken() should clear tokens and rethrow on failure', () => {
    localStorage.setItem('hcms_refresh_token', 'stale-refresh');

    let caught: unknown;
    service.refreshToken().subscribe({ error: e => (caught = e) });

    httpMock.expectOne(`${environment.apiUrl}/auth/refresh`).flush(
      { message: 'invalid refresh token' },
      { status: 401, statusText: 'Unauthorized' }
    );

    expect(caught).toBeTruthy();
    expect(localStorage.getItem('hcms_access_token')).toBeNull();
    expect(localStorage.getItem('hcms_refresh_token')).toBeNull();
  });
});
