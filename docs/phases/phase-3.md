# Phase 3 — Documents, Tasks & Notes

**Goal:** File upload to Azure Blob Storage is working. Task management within cases is complete.

**Duration:** 2 weeks  
**Status:** `[~]` In Progress — backend + frontend complete; E2E testing pending  
**Last Updated:** 2026-07-01

---

## Backend Tasks

- [x] Add `Documents`, `CaseTasks` EF Core entities; create migration
- [x] Implement `DocumentsController`:
  - [x] `POST /upload` — receive multipart form, validate file type and size (max 20 MB), upload to Blob, save metadata to DB
  - [x] `GET /{id}/download-url` — generate 1-hour SAS URL; log download to audit
  - [x] `DELETE /{id}` — soft-delete metadata; physical blob deletion deferred
- [x] Configure Azure Blob Storage client via `DefaultAzureCredential` (Managed Identity); container: `case-documents`
- [x] Implement `IBlobStorageService` abstraction in Application layer; concrete implementation in Infrastructure
- [x] Validate allowed MIME types: `application/pdf`, `image/png`, `image/jpeg`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- [x] Implement `TasksController` (task CRUD, status transitions, `GET /my` for current user's tasks)
- [x] Validate task status transitions server-side
- [x] Add task `DueDate` index for overdue query performance

## Frontend Tasks

- [x] Implement `DocumentsModule` components (embedded in Case Detail and Patient Detail tabs):
  - [x] `CaseDocumentsComponent` (`documents.component.ts`): table with file icon, name, size, category, uploader; download (SAS URL); delete; progress bar on upload
  - [x] Upload section: file picker, category select, description, progress bar
  - [x] `CaseService` extended with `getDocuments`, `uploadDocument`, `getDocumentDownloadUrl`, `deleteDocument`
- [x] Implement `CaseTasksComponent` (`tasks/tasks.component.ts`) — embedded in Case Detail tasks tab:
  - [x] Add task form: title, priority select, assignee dropdown, due date
  - [x] Task table: inline status select, overdue highlight, done/cancelled rows dimmed
  - [x] `CaseService` extended with `getTasks`, `addTask`, `updateTaskStatus`, `deleteTask`
- [ ] `MyTasksComponent` — standalone page for tasks assigned to current user (not yet built)
- [ ] Display overdue task count badge in sidebar nav

## Definition of Done

- [ ] User can upload a PDF to a case; file appears in document list (E2E test pending — requires Azurite running)
- [ ] Download generates a working SAS URL that expires (verify by waiting or inspecting expiry)
- [ ] Task can be created with an assignee and due date; status transitions work
- [ ] Overdue tasks are highlighted in red (E2E test pending)
- [ ] Deleted documents do not appear in the list but remain in audit log
