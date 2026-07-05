import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NotificationService } from '../../services/notification.service';
import { NotificationDto } from '../../models/notification.models';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
    MatMenuModule,
    MatDividerModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.scss']
})
export class NotificationBellComponent implements OnInit {
  notifications: NotificationDto[] = [];
  loading = false;
  unreadCount = 0;

  constructor(private notificationSvc: NotificationService) {}

  ngOnInit(): void {
    this.notificationSvc.unreadCount.subscribe(count => {
      this.unreadCount = count;
    });
  }

  onMenuOpen(): void {
    this.loading = true;
    this.notificationSvc.getNotifications().subscribe({
      next: res => {
        this.notifications = res.items;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  markRead(notification: NotificationDto, event: Event): void {
    event.stopPropagation();
    if (notification.isRead) return;

    this.notificationSvc.markRead(notification.id).subscribe(() => {
      notification.isRead = true;
      this.notificationSvc.decrementUnread(1);
    });
  }

  markAllRead(): void {
    this.notificationSvc.markAllRead().subscribe(() => {
      this.notifications.forEach(n => n.isRead = true);
      this.notificationSvc.clearUnread();
    });
  }

  trackById(_: number, n: NotificationDto) {
    return n.id;
  }
}
