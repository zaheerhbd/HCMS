# Phase 4 — Appointments & Scheduling

**Goal:** Appointments can be scheduled, confirmed, and completed. Conflict detection prevents double-booking.

**Duration:** 2 weeks  
**Status:** `[ ]` Not Started  
**Last Updated:** 2026-06-24

---

## Backend Tasks

- [ ] Add `Providers`, `Appointments` EF Core entities; create migration
- [ ] Implement `AppointmentsController`:
  - [ ] `POST /` — create appointment; validate provider has no overlapping appointment
  - [ ] `GET /` — paginated list filtered by patient, provider, status, date range
  - [ ] `PUT /{id}` — reschedule (re-validate conflicts)
  - [ ] `PUT /{id}/status` — Confirm, Complete, Cancel, NoShow
  - [ ] `GET /providers/{providerId}/availability` — return busy slots for a date range
- [ ] Seed initial providers (linked to clinician users)
- [ ] Background Hosted Service: query appointments starting in 24h with `ReminderSentAt IS NULL`; create `Notifications` rows; mark `ReminderSentAt`

## Frontend Tasks

- [ ] Implement `AppointmentsModule` (lazy-loaded):
  - [ ] `AppointmentListComponent`: paginated table, filter by patient/provider/status/date
  - [ ] `AppointmentFormComponent`: patient picker, provider picker, type selector, date/time range picker, conflict warning
  - [ ] `AppointmentDetailComponent`: status actions (Confirm, Complete, Cancel, NoShow)
  - [ ] `ProviderAvailabilityComponent`: simple slot grid for selected provider + date
  - [ ] `AppointmentService`: all HTTP calls
- [ ] Add upcoming appointments section to Case Detail

## Definition of Done

- [ ] Appointment can be scheduled and confirmed
- [ ] Attempting to book a provider at an occupied time shows a conflict error
- [ ] Status transitions (NoShow, Complete, Cancel) work
- [ ] A notification row is created for appointments starting in < 24h (verify via DB query)
