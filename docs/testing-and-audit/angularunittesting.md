# Angular — Unit Tests

**Scope:** Focus on the services and interceptors — they contain business logic. Components that are pure presentation (just binding) get lower priority. Aim for the hard-to-see-break things: token refresh, role checks, form validation errors.

## Services to Test

| Service / Directive | Detail | Effort |
|---|---|---|
| `AuthService` — login stores tokens, logout clears localStorage, `isLoggedIn$` emits | Mock HttpClient via `HttpClientTestingModule`. Verify localStorage writes, JWT decode, role extraction | High |
| `JwtInterceptor` — attaches Bearer header, triggers refresh on 401 | Use `HttpClientTestingModule`. Test with expired token → 401 → refresh → retry. Verify infinite loop prevention (skip `/auth/refresh`) | High |
| Reactive form validators — `PatientFormComponent`, `CaseFormComponent` | Date of birth ≤ today, required fields, MRN pattern (read-only). Test invalid vs valid states | Medium |
| `HasRoleDirective` — hides element when role doesn't match | Create a `TestHostComponent` with the directive. Verify ngIf equivalent behavior for each role | Low |
| `NotificationService` — unread count BehaviorSubject, polling interval | Mock HttpClient, use `fakeAsync`/`tick` for 60s polling. Verify badge count updates | Medium |

## Example: JwtInterceptor test

```typescript
describe('JwtInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true }
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    spyOn(authService, 'getToken').and.returnValue('valid.jwt.token');
  });

  it('should attach Authorization header', () => {
    http.get('/api/patients').subscribe();
    const req = httpMock.expectOne('/api/patients');
    expect(req.request.headers.get('Authorization'))
      .toBe('Bearer valid.jwt.token');
    req.flush({});
  });
});
```
