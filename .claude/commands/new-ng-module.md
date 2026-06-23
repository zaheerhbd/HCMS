Scaffold a new Angular **lazy-loaded feature module** for the HCMS web app.

Module name: **$ARGUMENTS**

## What to create

Under `src/hcm-web/src/app/features/<module-name>/`:

```
<module-name>/
├── <module-name>.module.ts          # NgModule with RouterModule.forChild()
├── <module-name>-routing.module.ts  # Child routes
├── components/
│   └── <module-name>-list/
│       ├── <module-name>-list.component.ts
│       ├── <module-name>-list.component.html
│       └── <module-name>-list.component.scss
└── services/
    └── <module-name>.service.ts     # HttpClient calls to /api/<module-name>
```

## Conventions

- **Lazy loading:** The module must be loaded via `loadChildren` in `AppRoutingModule` — never eagerly imported.
- **Reactive Forms** throughout — no template-driven forms.
- **Angular Material** for all UI components (mat-table, mat-card, mat-button, etc.).
- **`HasRoleDirective`** from `SharedModule` for any role-gated UI elements — never rely solely on route guards to hide content.
- **RxJS** for local component state (BehaviorSubject, async pipe) — do not add NgRx store slices unless the feature needs cross-module shared state.
- Services use `HttpClient` and return `Observable<T>` — no Promises.
- Add `AuthGuard` (and `RoleGuard` if role-restricted) to the route definition.

## After scaffolding

1. Wire the lazy route in `src/hcm-web/src/app/app-routing.module.ts`.
2. Add a sidebar nav link in `AppShellComponent` (guarded by `HasRoleDirective` if role-restricted).
3. Show me the full file tree of what was created.
