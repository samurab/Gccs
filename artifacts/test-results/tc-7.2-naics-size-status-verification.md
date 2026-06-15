# Story 7.2 NAICS And Size Status Verification

Date: 2026-06-15

## Scope

Implemented multiple NAICS codes on the company profile, primary NAICS normalization, per-code size status and basis, and profile gap warnings for missing size status.

## Test Cases

- TC-7.2.1: Passed. Multiple valid NAICS codes persist on the profile.
- TC-7.2.2: Passed. Primary NAICS can be switched and only one code remains primary.
- TC-7.2.3: Passed. Size status and basis are stored independently per NAICS.
- TC-7.2.4: Passed. A NAICS without size status appears in profile validation gaps.

## Commands

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~CompanyProfileTests"
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj
npm --workspace apps/web run test:run
npm --workspace apps/web run lint
npm --workspace apps/web run build
npm test
curl -I http://127.0.0.1:5173/
```

## Results

- Focused profile API tests: Passed, 8/8.
- API regression suite: Passed, 343/343.
- Web tests: Passed, 10/10.
- Web lint: Passed.
- Web build: Passed.
- Root npm test: Passed.
- Local Vite route response: 200 OK.

## Notes

Playwright browser smoke verification remains blocked because the local Playwright Chromium binary is not installed in this environment.
