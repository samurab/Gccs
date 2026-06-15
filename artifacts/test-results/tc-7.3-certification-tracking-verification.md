# Story 7.3 Certification Tracking Verification

Date: 2026-06-15

## Scope

Implemented company profile certification tracking for 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, SDB, and custom certifications. Certification saves now normalize expired/expiring status, generate renewal tasks for upcoming expirations, write certification audit events, and expose certification rows in the profile UI.

## Test Cases

- TC-7.3.1: Passed. Supported and custom certification types persist and display in the profile API; the web profile form submits certification rows.
- TC-7.3.2: Passed. A certification expiring within 90 days creates an open renewal calendar task.
- TC-7.3.3: Passed. An expired certification is returned with Expired status for profile display.
- TC-7.3.4: Passed. Create, update, and remove certification saves write CompanyCertification audit events.

## Commands

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~CompanyProfileTests"
npm --workspace apps/web run test:run -- App.test.tsx
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj
npm --workspace apps/web run lint
npm --workspace apps/web run test:run
npm --workspace apps/web run build
npm test
npm --workspace apps/web run dev -- --host 127.0.0.1 --port 4173
```

## Results

- Focused profile API tests: Passed, 12/12.
- Focused web profile tests: Passed, 11/11.
- API regression suite: Passed, 347/347.
- Web lint: Passed.
- Web tests: Passed, 11/11.
- Web build: Passed.
- Root npm test: Passed.
- Local Vite smoke: Vite served the app at http://127.0.0.1:4173/. Without the API running behind the browser session, the live profile route fell back to dashboard permissions; profile certification UI behavior is covered by React Testing Library.

## Notes

The existing company certification model does not yet include first-class evidence link fields, so this story stores issuer, status, effective/expiration dates, and reference number. Evidence attachment linkage can be added when the evidence vault and profile certification models are connected.
