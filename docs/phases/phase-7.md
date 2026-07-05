# Phase 7 — Notifications & Audit UI

**Goal:** In-app notification bell works. Audit log is browsable by admins.

**Duration:** 1 week  
**Status:** `[x]` Complete  
**Last Updated:** 2026-07-04

---

## Backend Tasks

- [x] Create `Notification` entity (`UserId`, `Type`, `Title`, `Message`, `EntityType`, `EntityId`, `IsRead`, `CreatedAt`, `ReadAt`)
- [x] Add `AddNotifications` EF Core migration; applied to DB
- [x] Implement `INotificationDataHandler` / `NotificationDataHandler`: `CreateAsync`, `GetMyNotificationsAsync`, `GetUnreadCountAsync`, `MarkReadAsync`, `MarkAllReadAsync`
- [x] Wire notification creation in `CaseDataHandler`: fire `CaseAssigned` on case creation (when assignee ≠ creator); fire `CaseEscalated` on status change to "Escalated"
- [x] Wire notification creation in `TaskDataHandler`: fire `TaskAssigned` on create and reassign
- [x] Add `NotificationOverdueService` (BackgroundService): runs every 24h, scans for overdue incomplete tasks, fires `TaskOverdue` notifications (deduped per day)
- [x] Implement `NotificationsController` at `/api/notifications`: `GET /` (list), `GET /unread-count`, `POST /{id}/read`, `POST /read-all`
- [x] Implement `IAuditDataHandler` / `AuditDataHandler`: paginated + filtered query with username enrichment
- [x] Implement `AuditController` at `/api/audit` (Admin only): filters by `entityType`, `action`, `changedBy`, `from`, `to`

## Frontend Tasks

- [x] Add `NotificationDto`, `AuditLogDto` models to `core/models/notification.models.ts`
- [x] Implement `NotificationService`: HTTP calls + 60s interval polling via `startPolling()`, `BehaviorSubject<number>` for unread count
- [x] Implement `NotificationBellComponent`: bell icon in top bar with badge; `mat-menu` dropdown with latest 20 notifications; individual + mark-all-read actions; unread dot indicator
- [x] Wire `NotificationBellComponent` into `AppShellComponent` topbar; start polling on `ngOnInit`
- [x] Implement `AuditLogComponent` (Admin only): filterable paginated Material table; expandable rows showing Before/After JSON diff; `entity type`, `action`, `changedBy`, date range filters; color-coded action badges
- [x] Convert Admin route to lazy-loaded `ADMIN_ROUTES`; add `/admin/audit` child route

## Definition of Done

- [x] Notification bell updates without page refresh
- [x] Notifications are created when a case is assigned
- [x] Admin can filter audit log by user and see JSON old/new values
- [x] Non-admin users cannot access `/api/audit` (403 returned)
- [x] API build: 0 errors; Angular production build: 0 errors
