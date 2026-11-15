# PROD2 database maintenance deployment status

## Decision

**NO-GO for live database deployment.** The implementation adds an interactive clone rehearsal. Both migration guards continue to reject these changes through ordinary deployment, and the host menu has no live maintenance action.

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

## Rehearsal implementation

The helper's separate `--maintenance-rehearse SHA` operation requires a terminal, a yes/no confirmation and the full SHA typed again. It checks exact CI and installed tool identity before the prompt and rechecks Git, CI, active Compose and container identity, approval, review and environment after approval, build and rehearsal. It uses the same preflight checks as ordinary release preparation. It does not change the approval file.

The first supported changes are new EF migration source/designer pairs and existing files listed in `gf3_maintenance.py::RELATED`. CI reviewers must confirm the exact nullable `TEXT` and `INTEGER` additions. Create a root-owned mode-0600 `/etc/gfctl/maintenance-review.json` inside the protected directory. Example only:

```json
{
  "before": "<full active PROD2 SHA>",
  "target": "<full reviewed PROD2 tip SHA>",
  "add_nullable_columns": [
    ["shift_correction_request", "comment", "TEXT"],
    ["shift_swap_request", "refreshed_at_utc", "INTEGER"]
  ]
}
```

The example describes the two SQL changes, but it does not authorize the combined PROD2 application release. Its diff includes out-of-scope application files and the classifier refuses it. A reviewed tooling baseline must already be active before a later migration-only release can qualify.

The build uses a clean detached worktree without the production environment file. The Docker image ID is recorded in the private audit. The temporary SQLite copy comes from the existing local named Docker volume through SQLite's read-only backup API. The container gets only that copy, synthetic configuration, no network or published ports, a read-only root filesystem and no Linux capabilities. The rehearsal checks startup health, exact EF history, full integrity, approved schema changes and logical row preservation. It removes the container and copies, and keeps a mode-0600 result under `/var/lib/gf3-deploy/maintenance-*/rehearsal.json`. If Docker cannot confirm cleanup, retain the private clone directory and inspect the corresponding disposable container locally. An interrupted rehearsal never authorizes deployment.

## Review and install rehearsal tools

Apply these steps only in a separate tooling review after Linux rehearsal CI passes and the installed host paths and baseline are confirmed. Start from a root-only staging and backup directory. Reuse the ownership checks, lock order and atomic `replace_file` procedure in [the existing gfctl installation runbook](gfctl-prod2-rollout.md). Keep both locks while checking and replacing tools.

1. Review the exact PROD2 tooling commit and confirm its push CI and required jobs. Fetch the ref only. Verify the new files and the unchanged Guard program and units against that reviewed commit. Obtain the SHA-256 manifest for the three reviewed Python files through the tooling review and keep it outside the checkout.
2. Write the reviewed Git blobs into the private staging directory with `git show "$reviewed_sha:deploy/security/$file"` for `gf3_prepare_release.py`, `gf3_release.py` and `gf3_maintenance.py`. Check each staged blob with `git hash-object --no-filters` against `git rev-parse "$reviewed_sha:deploy/security/$file"` and run `sha256sum -c` against the independent reviewed manifest. Compile the staged files with `python3 -m py_compile`.
3. Confirm `/usr/local/libexec/gf3` and each existing destination pass the runbook's root ownership, symlink and permissions checks. Save each installed file and its mode in the private backup directory and record absent destinations. Atomically replace the reviewed files as root, mode 0644, and verify the installed checksums again before releasing either lock. If a replacement or checksum check fails, restore the recorded tools with the same locks and verify the prior hashes.
4. After verifying the tool paths and available action number in the host-local menu, a separate menu review may add **Validate PROD2 DB migration on a copy**. Prompt for the target SHA and invoke `/usr/bin/python3 -I /usr/local/libexec/gf3/gf3_prepare_release.py --maintenance-rehearse "$sha"` with the terminal attached. Preserve its exit code and print success only if the helper reports a passed clone rehearsal. Install that menu change through the existing checksum and backup procedure.

These steps install rehearsal tooling outside the live Git checkout. Establishing a new active tooling baseline is a separate reviewed release operation. The ordinary helper continues to classify deployment scripts as critical and there is no tooling-and-migration exception.

## Operator runbook while live deployment is disabled

- Do not use a release operation to apply the two pending migrations. The ordinary `[11]` action remains the only menu action and its existing critical-file rejection is expected.
- Do not edit, copy, or restore the production SQLite database manually. The normal release rollback bundle is not a database-migration rollback guarantee.
- For an unrelated ordinary release, continue to use the installed, reviewed host tooling and its existing runbook. Do not install changed security tooling from this proposal.
- The rehearsal review and install procedure applies only to the clone operation. Live maintenance installation remains unavailable.
- The eventual operator should verify the working database using SQLite `PRAGMA integrity_check`, confirm the expected `__EFMigrationsHistory` rows, and exercise application health before ingress reopens. The final backup should be inspectable as a root-only artifact under a private maintenance bundle. Those commands and paths must be supplied by the reviewed implementation; they are not established by the current host tooling.
- Before traffic reopens, a failed migration may be rolled back only by restoring the verified final snapshot while ingress and every writer remain stopped, then verifying the database and previous app. After ingress reopens or any write may have happened, do not restore the snapshot; stop for manual intervention or forward repair.

## Live enablement gate

- `EmployeeNotificationCleanupHostedService.ExecuteAsync()` can write before the first request. `EmployeeWebPushHostedService.ScanAsync()` can also write. The clone disables delivery and fails if any startup job changes existing rows. Live staging needs a reviewed way to start the candidate without background database writers, then start those writers at the commit boundary.
- `gf3_release.py::start_nginx()` publishes ports before `finish_activation()` runs Guard and Git checks. A live maintenance transaction needs an ingress fence that stays closed during candidate HTTPS and Guard verification and across an interrupted process. Neither a stopped timer nor a successful SQLite `BEGIN IMMEDIATE` proves that all external writers will remain excluded after the lock is released.
- The quiesced snapshot primitive takes a SQLite writer lock and checks migration history and integrity. The tests verify recovery into a fresh file. Live replacement requires durable fencing of ingress and all writers, validation against the original Docker volume and a persisted no-automatic-restore boundary. There is no live restore command or one-use live deployment authorization yet.
