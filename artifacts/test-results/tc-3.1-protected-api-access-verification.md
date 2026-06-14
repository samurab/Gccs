# TC-3.1 Protected API Access Verification

Executed at: 2026-06-13T23:41:41.6599500+00:00

## Setup Data

- Repository: `/Users/devups/Development/CodexProjects/Gccs`
- App host: ASP.NET Core API via `WebApplicationFactory<Program>`
- Environment: Development
- Local dependencies: disabled with `LocalDependencies:Enabled=false`
- Persistence: EF Core InMemory database `tc-3-1-protected-api-access-20260613234141`
- Protected endpoints used: `GET /api/compliance/overview`, `GET /api/me/access`, `POST /api/tenants`
- Authentication: local development headers (`X-Gccs-Dev-Auth`, `X-Gccs-Dev-Tenant`, `X-Gccs-Dev-User`, `X-Gccs-Dev-Email`)
- TC-3.1.2 tenant ID: `33333333-3333-3333-3333-333333333331`
- TC-3.1.2 user ID: `44444444-4444-4444-4444-444444444441`
- TC-3.1.4 audit actor user ID: `55555555-5555-5555-5555-555555555551`

## Results

| Test case | Step | Expected result | Actual result | Outcome | Notes |
| --- | --- | --- | --- | --- | --- |
| TC-3.1.1 | Send GET /api/compliance/overview without X-Gccs-Dev-Auth or bearer token and with X-Correlation-ID tc-3-1-1-unauthenticated. | API returns 401 application/problem+json with errorCode authentication_required and preserves the request correlation ID. | HTTP 401 Unauthorized; contentType=application/problem+json; X-Correlation-ID=tc-3-1-1-unauthenticated; body={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Authentication required","status":401,"detail":"Authentication is required to access this tenant-scoped API.","errorCode":"authentication_required","traceId":"tc-3-1-1-unauthenticated","correlationId":"tc-3-1-1-unauthenticated"} | Pass | Representative protected endpoint rejected the request before handler execution. |
| TC-3.1.2 | Send GET /api/me/access with X-Gccs-Dev-Auth true, explicit tenant ID, explicit user ID, and developer email. | Handler receives and returns the expected tenantId, userId, and userEmail with 200 OK. | HTTP 200 OK; contentType=application/json; X-Correlation-ID=tc-3-1-2-context; body={"tenantId":"33333333-3333-3333-3333-333333333331","userId":"44444444-4444-4444-4444-444444444441","userEmail":"story-3-1@example.com","roles":["Owner"],"permissions":["ApproveEvidence","AuditorReadOnly","ManageCmmc","ManageCompanyProfile","ManageContracts","ManageEvidence","ManageObligations","ManageReports","ManageSubcontractors","ManageTasks","ManageTenant","ManageUsers","ViewAuditLog","ViewCmmc","ViewCompanyProfile","ViewContracts","ViewEvidence","ViewObligations","ViewRe...; tenantMatched=True; userMatched=True; emailMatched=True | Pass | The protected handler surfaced the current request context returned by ITenantContext. |
| TC-3.1.3 | Send GET /api/me/access with valid dev auth and user context, but X-Gccs-Dev-Tenant none. | API returns the standard missing tenant problem response: 400 application/problem+json with errorCode missing_tenant_context. | HTTP 400 BadRequest; contentType=application/problem+json; X-Correlation-ID=tc-3-1-3-missing-tenant; body={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Tenant context required","status":400,"detail":"An active tenant context is required for this tenant-scoped API request.","errorCode":"missing_tenant_context","traceId":"tc-3-1-3-missing-tenant","correlationId":"tc-3-1-3-missing-tenant"} | Pass | The standard exception handler converted the missing tenant context into a problem details response. |
| TC-3.1.4 | Send POST /api/tenants with valid dev auth, a request body, and X-Correlation-ID tc-3-1-4-success-audit; inspect the persisted audit entry. | Successful response includes X-Correlation-ID and the compliance audit log metadata stores the same correlation ID. | HTTP 201 Created; contentType=application/json; X-Correlation-ID=tc-3-1-4-success-audit; body={"id":"e202a1bd-321c-40d9-9413-6fa2a8d683dc","displayName":"Story 3.1 Correlation Tenant","status":"Active","dataPosture":"NoCui","trialEndsAt":null,"createdAt":"2026-06-13T23:41:41.884935+00:00","updatedAt":null}; auditEntryCount=1; auditContainsCorrelation=True | Pass | Tenant creation emitted an audit log entry with request metadata. |
| TC-3.1.4 | Send unauthenticated GET /api/compliance/overview with X-Correlation-ID tc-3-1-4-failed-response, then inspect API response and audit storage. | Failed API response includes the request correlation ID. Failed-request logging should also make the correlation ID available. | HTTP 401 Unauthorized; contentType=application/problem+json; X-Correlation-ID=tc-3-1-4-failed-response; body={"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Authentication required","status":401,"detail":"Authentication is required to access this tenant-scoped API.","errorCode":"authentication_required","traceId":"tc-3-1-4-failed-response","correlationId":"tc-3-1-4-failed-response"}; auditEntryCountAfterFailedRequest=1; failedAuditContainsCorrelation=False; capturedFailedLogCount=1; failedLogContainsCorrelation=True | Pass | Failed response included the correlation ID, and the verifier found a failed-request log record containing that same ID. |

## Defects Or Missing Coverage

- None found in this verification run.

## Coverage Notes

- This script verifies development authentication context, not production JWT validation.
- It uses representative protected endpoints rather than enumerating every protected route.
- Failed authentication responses are verified for correlation ID in headers and problem details. App-level audit logging for rejected unauthenticated requests is reported separately above.
- The verifier captures in-process `ILogger` entries and app audit rows; it does not inspect external host, reverse proxy, or cloud logging sinks.
