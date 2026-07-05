import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, interval, Subscription } from 'rxjs';
import { switchMap, startWith } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { NotificationDto, NotificationListDto, UnreadCountDto } from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService implements OnDestroy {
  private readonly base = `${environment.apiUrl}/notifications`;

  private unreadCount$ = new BehaviorSubject<number>(0);
  readonly unreadCount = this.unreadCount$.asObservable();

  private pollSub?: Subscription;

  constructor(private http: HttpClient) {}

  // Start polling every 60 seconds — called by AppShell after login
  startPolling(): void {
    this.stopPolling();
    this.pollSub = interval(60_000)
      .pipe(
        startWith(0),
        switchMap(() => this.http.get<UnreadCountDto>(`${this.base}/unread-count`))
      )
      .subscribe({
        next: res => this.unreadCount$.next(res.count),
        error: () => {}
      });
  }

  stopPolling(): void {
    this.pollSub?.unsubscribe();
    this.pollSub = undefined;
  }

  getNotifications() {
    return this.http.get<NotificationListDto>(this.base);
  }

  markRead(id: number) {
    return this.http.post<void>(`${this.base}/${id}/read`, {});
  }

  markAllRead() {
    return this.http.post<void>(`${this.base}/read-all`, {});
  }

  // Decrement local badge immediately for responsive UX
  decrementUnread(by = 1) {
    const current = this.unreadCount$.value;
    this.unreadCount$.next(Math.max(0, current - by));
  }

  clearUnread() {
    this.unreadCount$.next(0);
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }
}
