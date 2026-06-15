# TC-3.2 Workspace Navigation Verification

Executed at: 2026-06-14T00:59:03Z

## Setup Data

- Repository: `/Users/devups/Development/CodexProjects/Gccs`
- Frontend: React/Vite app in `apps/web`
- API: ASP.NET Core API started with `dotnet run --project apps/api`
- API URL: `http://localhost:5062`
- API environment: Development
- API health: `GET /health` returned `status=ok`, `service=gccs-api`, and `dataPosture=No-CUI / compliance management only`
- Frontend URL: `http://localhost:3000`
- Default authenticated local user: development auth header user `developer@gccs.local`, default Owner-equivalent development permissions
- Restricted live role setup:
  - `VITE_GCCS_DEV_ROLE=Auditor npm --workspace apps/web run dev -- --host localhost --port 3000`
  - `VITE_GCCS_DEV_ROLE=Contributor npm --workspace apps/web run dev -- --host localhost --port 3000`
- Mocked route-state execution: `npm run test:web`
- Test runner result: Vitest `v4.1.8`, `1` test file passed, `5` tests passed
- Existing test file used as verification script: `apps/web/src/App.test.tsx`
- Browser smoke tool: Codex in-app Browser against the local Vite app
- Worktree note: `apps/web/src/App.tsx`, `apps/web/src/App.test.tsx`, `apps/web/styles/globals.css`, and docs files were already modified before this artifact was created.

## Results

| Test case | Step | Expected result | Actual result | Outcome | Notes |
| --- | --- | --- | --- | --- | --- |
| TC-3.2.1 | Start the API, start the Vite app as the default authenticated development user, open `http://localhost:3000/`, and inspect the first authenticated screen. | The first authenticated screen is the workspace/dashboard, not a marketing page. Primary workspace navigation is present. | Browser showed `h1=Dashboard`, document title `GCCS Compliance Workspace`, nav label `Primary workspace navigation`, current nav item `#/dashboard`, tenant context `developer@gccs.local` with `No-CUI / compliance management only`, and no marketing/pricing/landing page text. | Pass | Vitest also passed `TC-3.2.1 lands authenticated users in the workspace dashboard`. |
| TC-3.2.2 | Run `npm run test:web`; in the React verification script, use `userEvent.tab()` to reach the skip link, then focus each primary route link and activate it with `{Enter}`. Routes checked: Profile, Contracts, Obligations, Calendar, Evidence, CMMC, Subcontractors, Reports, Settings. | Keyboard-only navigation reaches each visible primary route. Focus lands on the route link, Enter activates it, route content appears, and active nav gets `aria-current=page`. | Vitest passed. The script verified focus on each link, Enter activation, expected route empty-state/settings content, and `aria-current=page`. | Pass | The in-app browser smoke confirmed the live route list and first screen, but its bridge did not reliably drive Tab focus. Keyboard sequencing evidence comes from Testing Library/user-event. |
| TC-3.2.3 | Render restricted navigation in the React verification script using `restrictedAccess` with only `ViewObligations` and `ViewReports`; inspect visible links. Then run live browser smoke with local restricted roles and direct-load `http://localhost:3000/#/settings`. | Restricted users only see permitted visible links. Hidden routes cannot be reached through visible navigation, and restricted direct routes do not render restricted content. | Vitest passed: Dashboard, Obligations, and Reports were visible; Profile, Contracts, and Settings were absent; tenant member/invitation loaders were not called. Live browser smoke with restricted role setup showed no `#/settings` link; direct `#/settings` changed to `#/dashboard`, `h1=Dashboard`, `settingsLinkCount=0`, and no `Team members` content. | Pass | Current live `Auditor` and `Contributor` role catalog permissions still expose most read-oriented workspace routes; Settings remains hidden because they lack `ManageUsers`, `ManageTenant`, and `ViewAuditLog`. |
| TC-3.2.4 | Run `npm run test:web`; mock route data states in `apps/web/src/App.test.tsx`: pending overview promise for loading, fallback overview for empty, rejected overview promise for failed state. | Loading, empty, and failed route data states display understandable UI. | Vitest passed. Loading text: `Loading workspace data`. Empty-state text: `API overview unavailable` and `Source data unavailable`. Failed-state alert: `Workspace data could not be loaded`. | Pass with coverage caveat | The mocked UI states are covered. See missing coverage note below for real fetch failure behavior. |

## Exact Commands Executed

```bash
dotnet run --project apps/api
npm --workspace apps/web run dev -- --host localhost --port 3000
npm run test:web
curl -sS http://localhost:5062/health
VITE_GCCS_DEV_ROLE=Auditor npm --workspace apps/web run dev -- --host localhost --port 3000
VITE_GCCS_DEV_ROLE=Contributor npm --workspace apps/web run dev -- --host localhost --port 3000
```

## Defects Or Missing Coverage

- Live API/schema defect observed during browser smoke: `GET /api/tenant-members` and `GET /api/tenant-invitations` returned/logged PostgreSQL `42P01` errors because `gccs.tenant_memberships` and `gccs.tenant_invitations` did not exist in the local database. `GET /health` still returned `status=ok`, so health does not currently catch this migration/schema gap. This did not block the workspace navigation assertions because the frontend API helper returned fallback arrays, but it can hide broken Settings/team-management data in local verification.
- Potential TC-3.2.4 integration gap: `apps/web/src/lib/api.ts` catches failed `fetch` and non-OK API responses inside `getJson` and returns fallback data. Because of that, normal local API/network failure appears to produce empty fallback UI rather than the route-level `Workspace data could not be loaded` alert. The failed-state UI passes only when the API module itself is mocked to reject. If the acceptance criterion requires real route data failures to show the failed-state alert, this should be treated as a product defect.
- Browser-only keyboard focus evidence is incomplete because the in-app browser bridge did not reliably move focus with Tab in this run. The authoritative keyboard-only evidence is the passing React Testing Library `userEvent.tab()` / `{Enter}` verification.

## Coverage Notes

- TC-3.2.2 verifies route activation for visible Owner/Admin-style primary navigation in the component test, not with a full browser Tab traversal.
- TC-3.2.3 has both mocked narrow-access coverage and live role-catalog smoke coverage. The live role catalog currently grants broad read permissions to Auditor and Contributor, so the strongest live hidden-route evidence is Settings.
- This run did not execute production JWT auth, SSO, or non-development role provisioning.
