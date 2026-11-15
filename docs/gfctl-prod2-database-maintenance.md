# PROD2 database maintenance deployment status

## Decision

**NO-GO: the maintenance deployment path is disabled and is not present in the menu.** Keep ordinary `[11] Deploy PROD2` unchanged. It must continue to reject database migrations. Do not invoke `gf3_release.py deploy` directly, bypass either migration check, edit the production database by hand, or install a helper for this proposal. No production host action is part of this change.

The target PROD2 history includes `20261114225324_AddShiftCorrectionComment` and `20261115135735_RefreshManualShiftOffer`. Both add nullable SQLite columns, but that does not make the current cutover safe for a database change.

## Independent blockers and rollback risk

1. `gf3_prepare_release.py::changed_paths()` classifies database and deployment-sensitive paths as manual-maintenance changes and stops before CI review, approval, or deployment. Removing only that check would leave the release engine's independent guard in place.
2. `gf3_release.py::stage()` compares `migration_fingerprint()` for the running release and candidate. A changed EF migration or `DatabaseMigrationStartup.cs` is rejected before build/cutover. That guard is intentionally unchanged.
3. `backup()` makes an online SQLite snapshot while the current application may still write, and checks it with `quick_check`. This is useful for ordinary release recovery; it is not the required final backup after writers have stopped, and it does not perform the requested full `integrity_check`.
4. `recover()` restores the prior Compose configuration and containers. It does not restore `SQLite.db`. `DatabaseMigrationStartup.ApplyAsync()` applies EF migrations automatically when the candidate app starts. Therefore a failed migration can leave the database changed while the old app is restored.
5. Current cutover has no explicit traffic-quiesce boundary. It starts the candidate app while Nginx remains part of the live stack, then starts/recreates Nginx and performs Guard and Git activation afterward. The current code does not prove that all request writers and background writers stayed stopped through those later checks.
6. `verify_tooling()` requires host-installed security scripts and Guard units to match the target commit. The ordinary rollout notes do not constitute a separately reviewed maintenance-tool bootstrap. An unreviewed helper cannot safely install itself into the live checkout.

These are independent gates. A helper-only bypass would still stop in `stage()`, and bypassing both would expose production data to a migration without a quiesced final backup or database-aware recovery.

## Required state machine before enabling a future mode

The following is a design contract, **not an available operator procedure**. Each transition must be persistent, SHA-scoped, audited, and fail closed. Any failed check returns to a safe stopped state; it must never print a success state for work that did not run.

| State | Required action and evidence | Failure handling |
| --- | --- | --- |
| Review | Confirm the exact current PROD2 tip, latest successful CI run and attempt plus required jobs, candidate and checkout SHA, clean/trusted Git state, active manifest, mode-0600 environment and approval files, pinned Nginx image, live topology/data volume, installed-tool hashes, and both deployment/Guard locks. Classify the complete before/target diff. | Reject stale or ambiguous CI, any changed gate, combined unrelated critical changes, deletes/renames, symlinks, submodules, or unreviewed paths. |
| Validate on DB clone | Build the exact candidate image. Take a consistent SQLite snapshot of the existing production volume, run full integrity and migration-history checks, and copy it to a private temporary directory. Run candidate startup migrations and health checks against only that clone with no network, no published ports, and no shared production Docker network. Verify the source volume is logically unchanged. | Stop and remove all clone containers/files; retain sanitized audit data. Never mount the production volume in the rehearsal container. |
| Backup | Warn about expected downtime. Stop external ingress first, drain/stop Guard and app/background writers, prove no other container or host process can write the named production volume, then make a final WAL-consistent snapshot. Require root ownership, a mode-0700 directory, mode-0600 files, full integrity, and verified migration history. Keep previous backups and release bundles. | Keep ingress closed and writers stopped if state is uncertain. Do not report a completed backup or continue. |
| Approve SHA | Re-read CI, checkout, Git cleanliness/origin, active manifest, environment/approval bytes and modes, topology, tool hashes, and locks. Show the downtime warning and exact target SHA. Require an interactive confirmation and a second entry of the full target SHA. Re-read all gates immediately after confirmation. | Any difference invalidates the approval and requires a fresh review. Never allow `--check-only`, noninteractive input, environment flags, or generic force options to authorize maintenance. |
| Deploy | Consume a one-use maintenance capability tied to the exact SHA and operation. The release engine independently validates it and the allowed diff. Keep ingress closed while the candidate starts, migrations run, full SQLite integrity/history checks and app health succeed, Guard checks complete, and Git activation is verified. | Before traffic is reopened, stop writers, restore only the verified final SQLite snapshot, verify integrity/history, then start and verify the prior app. If restore cannot be proven, remain offline for intervention. |
| Reopen traffic | This is the irreversible data boundary. Reopen ingress only after every pre-open check succeeds; record the boundary before the first public request can reach the candidate. | If traffic has reopened or writes may have occurred, never automatically restore an older database snapshot. Keep the service stopped or in maintenance mode and require manual intervention/forward repair. |

### Proposed sensitive-file scope

If implemented, the initial classifier should allow only regular tracked EF migration source/designer files under `DataAccessLayer/Migrations/`, `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs`, its model snapshot, `GF3.WebApi/Infrastructure/DatabaseMigrationStartup.cs`, and `GF3.Tests/DatabaseMigrationStartupTests.cs`. A migration source and its generated designer must be reviewed as one unit. The complete change set must contain only these reviewed data-layer paths and their explicitly paired migration files. New unrelated files, moves/renames, symlinks, submodules, and all combined changes are rejected.

Deployment/security scripts, systemd units, Dockerfiles and Compose, secrets/settings, application entrypoint/startup outside the migration component, networking, TLS, authorization/security, and permissions remain out of scope. Changing any of them requires its own reviewed and checksum-verified bootstrap procedure before a maintenance deploy can be considered.

## Operator runbook while disabled

- Do not use a release operation to apply the two pending migrations. The ordinary `[11]` action remains the only menu action and its existing critical-file rejection is expected.
- Do not edit, copy, or restore the production SQLite database manually. The normal release rollback bundle is not a database-migration rollback guarantee.
- For an unrelated ordinary release, continue to use the installed, reviewed host tooling and its existing runbook. Do not install changed security tooling from this proposal.
- No maintenance-mode host installation steps can be run yet. Before a future enablement, a separate reviewed bootstrap must install the exact approved helper/engine by checksum as root-owned files outside the live Git checkout, under the established locks, with a recoverable backup. It must preserve `safe_path()` protections and prove installed hashes before exposing a menu action.
- The eventual operator should verify the working database using SQLite `PRAGMA integrity_check`, confirm the expected `__EFMigrationsHistory` rows, and exercise application health before ingress reopens. The final backup should be inspectable as a root-only artifact under a private maintenance bundle. Those commands and paths must be supplied by the reviewed implementation; they are not established by the current host tooling.
- Before traffic reopens, a failed migration may be rolled back only by restoring the verified final snapshot while ingress and every writer remain stopped, then verifying the database and previous app. After ingress reopens or any write may have happened, do not restore the snapshot; stop for manual intervention or forward repair.

## Enablement gate

The path remains disabled until the clone runner, writer/ingress gate, final backup/restore transaction, traffic-reopen boundary, direct-engine capability validation, guarded host-tool bootstrap, signal/timeout cleanup, and stage-by-stage failure rehearsal are implemented and pass the required Linux Docker, SQLite, CI, .NET migration, and frontend checks. This repository is not evidence that production host behavior was tested.
