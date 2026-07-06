# RBAC Verification

| Endpoint | ReadOnly should get | CareCoordinator | Admin |
|---|---|---|---|
| `GET /api/patients` | 200 | 200 | 200 |
| `POST /api/patients` | 403 | 201 | 201 |
| `GET /api/audit` | 403 | 403 | 200 |
| `GET /api/dashboard/admin` | 403 | 403 | 200 |
| `DELETE /api/patients/{mrn}` | 403 | 403 | 200 |
| `POST /api/users` | 403 | 403 | 201 |

## Tasks

| Task | Detail | Effort |
|---|---|---|
| Verify AuditController returns 403 when called with non-Admin JWT | In integration tests: mint a CareCoordinator token → `GET /api/audit` → assert 403. This is already in the integration test plan — verify it's implemented | Low |
