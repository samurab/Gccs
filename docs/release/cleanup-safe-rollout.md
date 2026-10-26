# Cleanup-safe migration and API rollout

## Implemented in the prepared source

The staging and production workflow drafts check that cleanup is explicitly
disabled and the API reports `Stopped` before executing migration SQL. The read-only
guard rejects missing or ambiguous configuration, lookup failure, observed drift,
additional App Service apps in the resource group and deployment slots. It uses
an explicit subscription and suppresses provider diagnostics and setting values.
The guard observes state; it does not stop processes or change configuration.

API artifact deployment requests no automatic restart. The workflows check again
before starting the API, then verify `Running` with cleanup still disabled.
They contain no cleanup re-enable or failure rollback to the older consumer.
These are prepared changes, not evidence of a successful deployed rollout.

## Planned operating sequence

1. Bind the release to the exact reviewed source, immutable artifacts and protected
   environment. Resolve the separate clean-lineage and historical-evidence gates.
2. Reconcile the complete database/queue consumer inventory and maintenance window.
   The current guard supports one API App Service without slots. Review any other
   app, slot, worker host, scheduler or database consumer separately; resource-group
   discovery is not proof that no consumer exists elsewhere.
3. For every affected consumer, preserve its configuration and accepted recovery
   state, then set one unambiguous `ObjectCleanupProcessing__Enabled=false`
   override. Changing configuration alone does not prove an existing worker stopped.
4. Complete the controlled process stop and drain verification before migration or
   new-binary startup. Preserve queue rows and objects. Record the actual process,
   instance and timing evidence under its accepted custody scope.
5. Run the guarded forward migration and deploy the exact staged API bytes while
   retaining the pause. The workflow starts the API only after successful artifact
   deployment and a repeated guard. Startup itself may apply migrations, so it is
   inside the same paused interval.
6. Verify actual deployed binary identity, schema/migration metadata, health and
   dependencies across the accepted fleet. A release SHA in app settings or a
   successful configuration update alone does not prove the new binary loaded.
7. Leave cleanup disabled if any phase fails or evidence is missing. Do not run
   migration `Down`, delete queue rows or automatically restore the legacy consumer.
8. Re-enable cleanup only through a separately reviewed successful transition after
   all consumers run the corrected implementation. Its registration is evaluated
   at startup, so plan the restart and authorized synthetic verification separately.

## Partially implemented and do not claim

Separate Azure reads are not an atomic lock. State can change between observations;
external rollout serialization and the explicit disable override remain important.
`Stopped` is the provider-reported state, not independent forensic process proof.
The guard does not authenticate complete fleet ownership, establish backup/restore
success, validate customer data or grant file/object deletion authority.

This procedure requires a controlled interruption of API availability. It does not
provide a zero-downtime or multi-app/slot rollout. Staging and production retain
their source, evidence, protected-environment and exact publication approvals.
