import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { NotificationService } from './notification.service';
import { environment } from '../../../environments/environment';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [NotificationService]
    });

    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    service.stopPolling();
    httpMock.verify();
  });

  it('should be created with an initial unread count of 0', () => {
    let value = -1;
    service.unreadCount.subscribe(v => (value = v));
    expect(value).toBe(0);
  });

  it('startPolling() should fetch immediately and then every 60 seconds', fakeAsync(() => {
    service.startPolling();

    httpMock.expectOne(`${environment.apiUrl}/notifications/unread-count`).flush({ count: 3 });
    let value = -1;
    service.unreadCount.subscribe(v => (value = v));
    expect(value).toBe(3);

    tick(60_000);
    httpMock.expectOne(`${environment.apiUrl}/notifications/unread-count`).flush({ count: 5 });
    expect(value).toBe(5);

    service.stopPolling();
    tick(60_000);
    httpMock.expectNone(`${environment.apiUrl}/notifications/unread-count`);
  }));

  it('stopPolling() should unsubscribe and stop further polling requests', fakeAsync(() => {
    service.startPolling();
    httpMock.expectOne(`${environment.apiUrl}/notifications/unread-count`).flush({ count: 1 });

    service.stopPolling();
    tick(120_000);
    const pending = httpMock.match(`${environment.apiUrl}/notifications/unread-count`);
    expect(pending.length).toBe(0);
  }));

  it('decrementUnread() should reduce the count but never go below zero', () => {
    service.startPolling();
    httpMock.expectOne(`${environment.apiUrl}/notifications/unread-count`).flush({ count: 2 });
    service.stopPolling();

    let value = -1;
    service.unreadCount.subscribe(v => (value = v));

    service.decrementUnread();
    expect(value).toBe(1);

    service.decrementUnread(5);
    expect(value).toBe(0);
  });

  it('clearUnread() should reset the count to zero', () => {
    service.startPolling();
    httpMock.expectOne(`${environment.apiUrl}/notifications/unread-count`).flush({ count: 7 });
    service.stopPolling();

    service.clearUnread();
    let value = -1;
    service.unreadCount.subscribe(v => (value = v));
    expect(value).toBe(0);
  });

  it('markRead() should POST to the correct endpoint', () => {
    service.markRead(42).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/notifications/42/read`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('markAllRead() should POST to the read-all endpoint', () => {
    service.markAllRead().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/notifications/read-all`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
