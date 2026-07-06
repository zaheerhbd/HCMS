# E2E Tests — Playwright

**Why Playwright over Cypress?** Playwright runs on all browsers, has better async support, and a smaller bundle. For a .NET + Angular stack it's zero config — just `npm init playwright@latest` in the Angular project folder.

## Setup

```bash
# In src-ng/hcm-web/
npm init playwright@latest -- --quiet
# Choose: TypeScript, tests/ folder, no GitHub Actions (we'll add manually)
```

```typescript
// playwright.config.ts — point at Angular dev server
baseURL: 'http://localhost:4200',
webServer: {
  command: 'ng serve',
  url: 'http://localhost:4200',
  reuseExistingServer: !process.env.CI
}
```

## 4 Required E2E Flows

| Flow | Detail | Effort |
|---|---|---|
| Flow 1 — Login flow | Navigate to `/login`, fill username/password (`admin`/`Admin@123!`), click Sign In → assert dashboard route, assert notification bell visible, assert sidebar shows user name | Low |
| Flow 2 — Create patient | Login → `/patients/new` → fill all required fields → Submit → assert `MRN-YYYY-NNNNN` shown in success toast, patient appears in list | Medium |
| Flow 3 — Create case and change status | From PatientDetail → New Case → pick type/priority → submit → `CASE-YYYY-NNNNN` shown → click "Move to Open" → verify status badge updates | Medium |
| Flow 4 — Upload document to case | On CaseDetail → Documents tab → Upload → select a PDF file → confirm → assert file name appears in list with download link. Download link must be SAS URL (contains `?sv=`) | High |

## Example: Login flow test

```typescript
import { test, expect } from '@playwright/test';

test('login → dashboard', async ({ page }) => {
  await page.goto('/login');

  await page.fill('[formControlName="username"]', 'admin');
  await page.fill('[formControlName="password"]', 'Admin@123!');
  await page.click('button[type="submit"]');

  await expect(page).toHaveURL(/dashboard/);
  await expect(page.locator('mat-icon:text("notifications")'))
    .toBeVisible();
});

test('invalid creds shows error', async ({ page }) => {
  await page.goto('/login');
  await page.fill('[formControlName="username"]', 'admin');
  await page.fill('[formControlName="password"]', 'wrong');
  await page.click('button[type="submit"]');

  await expect(page.locator('snack-bar-container'))
    .toContainText('Invalid credentials');
  await expect(page).toHaveURL(/login/);
});
```
