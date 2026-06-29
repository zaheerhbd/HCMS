# Phase 7 — Notifications & Audit UI

**Goal:** In-app notification bell works. Audit log is browsable by admins.

**Duration:** 1 week  
**Status:** `[ ]` Not Started  
**Last Updated:** 2026-06-24

---

## Backend Tasks

- [ ] Ensure `Notifications` entity and migration exist (may be done in Phase 4)
- [ ] Wire notification creation in application layer for all trigger events: case assigned, task assigned, task overdue (daily check via Hosted Service), escalation, appointment reminder
- [ ] Implement `NotificationsController`: list, unread count, mark read, mark all read
- [ ] Implement `AuditController` (Admin only): paginated audit log with filters (entity type, user, date range, action)

## Frontend Tasks

- [ ] Implement `NotificationsModule`:
  - [ ] `NotificationBellComponent`: icon in top bar; polls `GET /notifications/unread-count` every 60s; badge
  - [ ] `NotificationListComponent`: dropdown/panel showing latest 20 notifications with mark-read action
  - [ ] `NotificationService`: HTTP calls + polling
- [ ] Implement `AdminModule` → `AuditLogComponent`:
  - [ ] Filterable, paginated table of all audit events
  - [ ] Filter by: entity type, user (autocomplete), date range, action
  - [ ] Row expand: show OldValues / NewValues JSON diff (basic formatted JSON view)

## Definition of Done

- [ ] Notification bell updates without page refresh
- [ ] Notifications are created when a case is assigned
- [ ] Admin can filter audit log by user and see JSON old/new values
- [ ] Non-admin users cannot access `/api/audit` (403 returned)
