# Phase 3 — Documents, Tasks & Notes

**Goal:** File upload to Azure Blob Storage is working. Task management within cases is complete.

**Duration:** 2 weeks  
**Status:** `[ ]` Not Started  
**Last Updated:** 2026-06-24

---

## Backend Tasks

- [ ] Add `Documents`, `CaseTasks` EF Core entities; create migration
- [ ] Implement `DocumentsController`:
  - [ ] `POST /upload` — receive multipart form, validate file type and size (max 20 MB), upload to Blob, save metadata to DB
  - [ ] `GET /{id}/download-url` — generate 1-hour SAS URL; log download to audit
  - [ ] `DELETE /{id}` — soft-delete metadata; physical blob deletion deferred
- [ ] Configure Azure Blob Storage client via `DefaultAzureCredential` (Managed Identity); container: `case-documents`
- [ ] Implement `IBlobStorageService` abstraction in Application layer; concrete implementation in Infrastructure
- [ ] Validate allowed MIME types: `application/pdf`, `image/png`, `image/jpeg`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- [ ] Implement `TasksController` (task CRUD, status transitions, `GET /my` for current user's tasks)
- [ ] Validate task status transitions server-side
- [ ] Add task `DueDate` index for overdue query performance

## Frontend Tasks

- [ ] Implement `DocumentsModule` components (embedded in Case Detail and Patient Detail tabs):
  - [ ] `DocumentListComponent`: table with name, category, uploader, date; download button (calls SAS URL endpoint)
  - [ ] `DocumentUploadComponent`: drag-and-drop or file picker, category selector, progress bar
  - [ ] `DocumentService`: upload with `HttpClient` `reportProgress`, download URL fetch
- [ ] Implement `TasksModule` (lazy-loaded):
  - [ ] `MyTasksComponent`: tasks assigned to current user with overdue highlighting
  - [ ] `TaskListComponent`: embedded in Case Detail tasks tab
  - [ ] `TaskFormComponent`: create/edit task, assignee picker, due date picker
  - [ ] `TaskStatusChipComponent`: clickable status chip triggering transition dialog
  - [ ] `TaskService`: all HTTP calls
- [ ] Add `FileUploadComponent` to `SharedModule` (reusable, emits `File` object to parent)
- [ ] Display overdue task count badge in sidebar nav

## Definition of Done

- [ ] User can upload a PDF to a case; file appears in document list
- [ ] Download generates a working SAS URL that expires (verify by waiting or inspecting expiry)
- [ ] Task can be created with an assignee and due date; status transitions work
- [ ] Overdue tasks are highlighted in red in `MyTasksComponent`
- [ ] Deleted documents do not appear in the list but remain in audit log
