# Phase 6 — Dashboards & Reports

**Goal:** All three dashboards are populated with live data. CSV export works for all three report types.

**Duration:** 2 weeks  
**Status:** `[ ]` Not Started  
**Last Updated:** 2026-06-24

---

## Backend Tasks

- [ ] Implement `DashboardController`:
  - [ ] `GET /coordinator` — my open cases (by status), my overdue tasks, today's appointments, cases with no activity in 7 days
  - [ ] `GET /supervisor` — team caseload per user, escalated cases, inactive cases
  - [ ] `GET /admin` — active user count, logins last 7 days, error rate from App Insights (or simple DB counts)
- [ ] Implement `ReportsController`:
  - [ ] `GET /cases-by-status` — JSON + CSV via `format=csv` query param; use `CsvHelper` NuGet
  - [ ] `GET /case-duration` — average days open→closed by case type
  - [ ] `GET /tasks-by-assignee` — done vs. overdue vs. open per user
- [ ] Add DB indexes to support dashboard queries efficiently (case status + coordinator, task due date + assignee)

## Frontend Tasks

- [ ] Implement `DashboardModule` (lazy-loaded):
  - [ ] `CoordinatorDashboardComponent`: stat cards (open cases, overdue tasks, today's appointments), cases-by-status donut chart, overdue task list
  - [ ] `SupervisorDashboardComponent`: team caseload bar chart, escalated cases table, inactive cases table
  - [ ] `AdminDashboardComponent`: user counts, login trend line chart, top error list
- [ ] Implement `ReportsModule` (lazy-loaded):
  - [ ] `CasesByStatusReportComponent`: bar chart + table + CSV export button
  - [ ] `CaseDurationReportComponent`: bar chart per case type + CSV export
  - [ ] `TasksByAssigneeReportComponent`: stacked bar chart + CSV export
- [ ] Install and configure `ng2-charts` + `Chart.js`
- [ ] Route dashboard based on user's primary role (default landing page post-login)

## Definition of Done

- [ ] Coordinator dashboard shows accurate counts matching DB state
- [ ] CSV export downloads a valid file with correct headers and data
- [ ] Charts render without console errors
- [ ] Supervisor can see their team's caseloads
