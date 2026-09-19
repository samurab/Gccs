# FeDril Versioning And Immutable Promotion

## Architecture Status

### Implemented

- `release/version.json` is the canonical application SemVer source.
- `apps/web/package.json` and `Directory.Build.props` must match the canonical version; CI executes `npm run check:release-control` to detect drift.
- Staging builds environment-neutral API and web artifacts once, generates the idempotent migration bundle and SPDX SBOM, records SHA-256 digests, deploys those bytes, and publishes candidate assets only after staging health succeeds.
- The web application loads public environment configuration from `runtime-config.js`. API URLs and Microsoft identity identifiers are no longer required to be compiled into the JavaScript bundle.
- `.github/workflows/production-release.yml` downloads the staged candidate assets, checks every digest against `docs/release/approved-release.json`, verifies protected candidate and final tags point to the same GitHub-verified main commit, and promotes the staged bytes without rebuilding them.
- Approved manifests and candidate artifacts receive GitHub build-provenance attestations; production verifies both attestations and recorded SHA-256 digests before making infrastructure changes.
- A successful protected production deployment publishes the final GitHub release record without replacing the candidate assets.
- The API `/health` response identifies the application version, candidate tag, final release tag, commit SHA, and deployment build ID.
- The historical `.github/workflows/production.yml` path is frozen to `launch-candidate-2026-09-13-1` for rollback only.
- The active GitHub repository ruleset `Immutable semantic release tags` blocks deletion and non-fast-forward updates for `refs/tags/v*`; both candidate and production workflows fail closed if that ruleset is absent or inactive.
- GitHub repository release immutability is enabled. Candidate and final releases are created as drafts, populated and verified, and only then published; published tags, release assets, and release metadata cannot be altered or deleted through the normal release workflow.
- The GitHub `staging` environment accepts deployments only from protected branches; the staging workflow deploys pushes to protected `main` and manually selected candidates that have already been proven to be ancestors of `main`.
- Production queries the live Azure PostgreSQL point-in-time recovery window and requires at least seven days of retention plus a reported earliest restore time before applying the staged migration bundle. This is a preflight control, not proof that a restore will succeed.

### Partially Implemented

- `docs/release/approved-release.json` is the governed release decision record. A `legacy-import` entry is rollback evidence only and intentionally cannot run the immutable production workflow; an `approved` entry must identify the exact staged artifacts and evidence.
- GitHub tag rulesets and protected-environment reviewers are external repository settings. The workflows verify a GitHub-signed merge commit and use the `staging` and protected `production` environments, but repository administrators must keep those controls enabled.
- Every SemVer production promotion requires a successfully staged candidate, recorded artifact digests, a protected final tag, an attested approval manifest, and protected production-environment approval.

### Planned

- No broader production launch, CUI processing, certification, government approval, or compliance guarantee is established by application versioning.
- Mirror immutable releases to an organization-controlled OCI registry only if independent retention, regional replication, or external key management becomes a requirement.

## Version Rules

Use `MAJOR.MINOR.PATCH`:

- Increment `PATCH` for backward-compatible fixes.
- Increment `MINOR` for backward-compatible product capability.
- Increment `MAJOR` for breaking API, permission, persisted-data, or customer-workflow changes.
- Use candidate tags in the form `vMAJOR.MINOR.PATCH-rc.N`.
- Use final release tags in the form `vMAJOR.MINOR.PATCH`.
- Candidate and final tags for one release must be immutable direct commit tags governed by the repository's `v*` tag ruleset. They must point to the same GitHub-verified commit on `main`.
- A version number is an identity and compatibility signal, not deployment approval. Only the approved release manifest and protected production environment grant deployment eligibility.

## Release Procedure

1. Update `release/version.json`, `apps/web/package.json`, the matching workspace entry in `package-lock.json`, and `Directory.Build.props` in one pull request.
2. Run `npm run check:release-control`, `npm run test:release-control`, the applicable web and API suites, and release-level verification.
3. Merge the verified change to `main`.
4. Create and push a protected candidate tag, for example:

   ```bash
   git tag v0.2.0-rc.1 <verified-main-sha>
   git push origin v0.2.0-rc.1
   ```

5. Dispatch `Staging deployment` with `candidate_tag=v0.2.0-rc.1`. The workflow validates tag protection assumptions, GitHub commit verification, main ancestry, and expand-only migration safety; then it builds once, generates the migration artifact and SBOM, deploys staging, verifies release identity and dependency health, attests the bundle, and publishes immutable prerelease assets.
6. Complete staging UAT, RBAC, tenant-isolation, No-CUI, backup/restore, migration, rollback, and approval evidence required by the production-readiness process.
7. If any application byte changes, increment the candidate number and repeat staging. Do not replace assets attached to an existing candidate.
8. Create and push the protected final tag on the exact candidate commit:

   ```bash
   git tag v0.2.0 <same-candidate-sha>
   git push origin v0.2.0
   ```

9. Update `docs/release/approved-release.json` from the candidate's `release-metadata.json`, SHA-256 values, staging run, approval record, and rollback target. Set `status` to `approved` and `artifacts.mode` to `immutable-promotion`.
10. Merge the approval PR after CI and required reviewers pass. Wait for `Approved release attestation` to attest the exact merged manifest.
11. Dispatch `Production deployment` with the exact final tag. Production verifies the manifest and artifact attestations, checks all digests, applies the staged migration bundle, adds the production runtime configuration overlay, deploys, and verifies `/health` against the approved identity.
12. Record the production run and health evidence. Do not rewrite or move either release tag.

## Approved Manifest Example

```json
{
  "schemaVersion": 2,
  "status": "approved",
  "version": "0.2.0",
  "candidateTag": "v0.2.0-rc.2",
  "releaseTag": "v0.2.0",
  "commitSha": "0123456789012345678901234567890123456789",
  "approvedAt": "2026-09-19",
  "approvalScope": "Solo-controlled No-CUI pilot production release",
  "dataPosture": "no-cui-only",
  "staging": {
    "environment": "staging",
    "runId": 123456789,
    "evidenceUrl": "https://github.com/samurab/Gccs/actions/runs/123456789"
  },
  "artifacts": {
    "mode": "immutable-promotion",
    "releaseAssetTag": "v0.2.0-rc.2",
    "api": { "name": "fedril-api.zip", "sha256": "<64 lowercase hex characters>" },
    "web": { "name": "fedril-web.zip", "sha256": "<64 lowercase hex characters>" },
    "migration": { "name": "fedril-migrations.zip", "sha256": "<64 lowercase hex characters>" },
    "sbom": { "name": "fedril-sbom.spdx.json", "sha256": "<64 lowercase hex characters>" },
    "metadata": { "name": "release-metadata.json", "sha256": "<64 lowercase hex characters>" }
  },
  "approvals": { "engineering": true, "security": true, "product": true },
  "rollback": { "sourceTag": "v0.1.0" }
}
```

## Hidden Risks And Dependencies

- Database rollback is not equivalent to application rollback. Candidate automation blocks destructive EF migration additions (`Drop*`, `AlterColumn`, `Rename*`, `DeleteData`, and raw `Sql`) and destructive standalone SQL additions. Contract cleanup requires a separate, reviewed release after the compatibility window so the previous application remains usable after the forward migration.
- `runtime-config.js` contains public identifiers and endpoints only. Secrets, tokens, connection strings, signing keys, and customer data must never be written into it or any web artifact.
- Repository owners can change the immutability policy for future releases, and repository availability remains a trust boundary. Published immutable releases, recorded SHA-256 digests, and GitHub attestations protect current release contents; independent disaster-recovery retention still requires an external mirror.
- Candidate publication requires GitHub artifact-attestation support, a GitHub-verified main commit, and the active immutable `v*` tag ruleset. Missing repository support for those controls blocks publication.
- Staging and production runtime configuration may differ, but the API, web code, migration scripts, and SBOM must remain byte-identical.
- Production requires `PRODUCTION_MSAL_CLIENT_ID`, `PRODUCTION_MSAL_TENANT_ID`, and `PRODUCTION_MSAL_API_SCOPE` before any database mutation is attempted. The four `PRODUCTION_CUSTOMER_MSAL_*` values remain optional until the production customer identity plane is explicitly provisioned; enabling that plane requires all four values plus the matching API authority and audience configuration.
