# Phase 4 — Appointments & Scheduling

**Goal:** Appointments can be scheduled, confirmed, and completed. Conflict detection prevents double-booking.

**Duration:** 2 weeks  
**Status:** `[~]` In Progress — backend + frontend complete; E2E testing pending  
**Last Updated:** 2026-07-01

---

## Backend Tasks

- [x] Add `Providers`, `Appointments` EF Core entities; create migration
- [x] Implement `AppointmentsController`:
  - [x] `POST /` — create appointment; validate provider has no overlapping appointment
  - [x] `GET /` — paginated list filtered by patient, provider, status, date range
  - [x] `PUT /{id}` — reschedule (re-validate conflicts)
  - [x] `PUT /{id}/status` — Confirm, Complete, Cancel, NoShow
  - [x] `GET /providers/{providerId}/availability` — return busy slots for a date range
- [x] Seed initial providers (linked to clinician users)
- [x] Background Hosted Service: query appointments starting in 24h with `ReminderSentAt IS NULL`; create `Notifications` rows; mark `ReminderSentAt`

## Frontend Tasks

- [x] Implement `AppointmentsModule` (lazy-loaded):
  - [x] `AppointmentListComponent`: paginated table, status filter chip, status colour-coded chips
  - [x] `AppointmentFormComponent`: patient MRN, provider select (with specialty hint), type selector, datetime-local range, conflict error display
  - [x] `AppointmentDetailComponent`: detail grid, status change section (CareCoordinator+)
  - [x] `AppointmentService`: all HTTP calls (`list`, `getById`, `create`, `changeStatus`, `getProviders`)
  - [ ] `ProviderAvailabilityComponent`: slot grid for selected provider + date (not yet built)
- [ ] Add upcoming appointments section to Case Detail

## Definition of Done

- [ ] Appointment can be scheduled and confirmed (E2E test pending)
- [ ] Attempting to book a provider at an occupied time shows a conflict error (E2E test pending)
- [ ] Status transitions (NoShow, Complete, Cancel) work (E2E test pending)
- [ ] A notification row is created for appointments starting in < 24h (verify via DB query)
