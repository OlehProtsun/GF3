# PROD2 database maintenance deployment status

## Decision

**Live installation requires separate review and authorization.** The repository implements the maintenance transaction behind the existing `[11] Deploy PROD2` action. A green disposable rehearsal qualifies the tested revision for review. Production compatibility and installation are distinct operator actions.

## Managed deployment through [11]

The helper detects appended EF migration pairs from the exact PROD2 diff. The ordinary deployment engine retains its migration fingerprint guard. Maintenance accepts ordinary application changes alongside complete generated migration pairs for nullable SQLite `TEXT` and `INTEGER` columns. It independently derives the reviewed column plan from the checked-out migration and compares schema, data and migration history after rehearsal. Unsupported critical files, existing migration edits and unsafe Git file modes block the update.

The helper builds the exact image and runs both the EF startup process and a read-only health host on a private database copy. It prints the SHA, the number of pending migrations, the rehearsal result and the downtime warning. The operator then confirms the maintenance and types the complete target SHA. The engine consumes a protected capability once after checking the exact tip, current release, installed tools, review digest, migration fingerprint, CI attempt and image ID again.

The one-time tooling transition may require a separate `/etc/gfctl/maintenance-tooling-review.json` record. It binds `before` and `target` SHAs to a `blobs` object containing the exact reviewed Git object ID of each changed member of `BOOTSTRAP_PATHS`. It must be root-owned and mode 0600. Routine schema updates derive the column plan from Git and require no per-deploy review file. The optional [13] operation retains its independent clone-only review file.

## Fence and recovery boundary

1. The release engine records a root-only journal at `/var/lib/gf3-deploy/maintenance-active.json`, takes the shared deployment lock and installs a dedicated `inet gf3_maintenance` nftables table. It blocks public inbound HTTP/HTTPS and forwarded access to the existing Docker bridge. GF3 stops Nginx first to close HTTP and SignalR connections, drains Guard, disables container restart policies and stops the old app.
2. The engine checks the kernel rule set, container mounts, restart policies and host SQLite file descriptors and mappings. It then takes a SQLite backup while holding a reserved writer lock. The backup, migration history, original file identities and SHA-256 are recorded in the journal. The full integrity check and logical row comparison must pass before migration. The original named volume and bridge are retained.
3. The candidate runs existing EF startup with no network, no published ports and no hosted background jobs. A separate read-only process then serves only health, using the same volume mounted read-only behind an Nginx instance with no published ports. The engine verifies schema, history, existing rows, TLS, ACME, topology, Guard and Git before it permits ordinary application startup.
4. The durable `WRITES_MAY_RESUME` transition is written **before** starting the ordinary writable application. The kernel fence stays in place through its health, HTTPS and Guard timer checks. The engine removes the fence only after these succeed and checks HTTPS again. Once the marker is written, a failed deployment cannot automatically restore SQLite, even if public ports were still blocked.
5. Before that boundary, a failure can restore the final backup if Git is still at the previous release, the kernel fence is intact, all writers are stopped and the original rows are intact. The journal consumes the restore transition before modifying the database. The engine retains the failed database and its journals, atomically installs the checksum-verified replacement, checks the original history and integrity, then verifies and reopens the prior release. Failed or unprovable recovery leaves `RECOVERY_REQUIRED` and the evidence on disk. [12] is code rollback only and refuses to cross a migration boundary.

An unfinished journal blocks managed deploy, code rollback and certificate renewal after process exit or reboot. A reboot can clear nftables state. The pre-open containers have restart policy `no`, and the engine never infers a safe rollback from the journal alone. Reconcile the stored phase, actual firewall, Git, Docker and SQLite state through a controlled incident procedure. In particular, do not delete the journal or restore a retained SQLite snapshot after the write boundary. The Linux disposable rehearsal simulates kernel fence loss and Docker restart separately from ordinary failure recovery.

The target PROD2 history includes `20261114225324_AddShiftCorrectionComment` and `20261115135735_RefreshManualShiftOffer`. Both add nullable SQLite columns, but that does not make the current cutover safe for a database change.

## Independent blockers and rollback risk

1. `gf3_prepare_release.py::changed_paths()` classifies database and deployment-sensitive paths as manual-maintenance changes and stops before CI review, approval, or deployment. Removing only that check would leave the release engine's independent guard in place.
2. `gf3_release.py::stage()` compares `migration_fingerprint()` for the running release and candidate. A changed EF migration or `DatabaseMigrationStartup.cs` is rejected before build/cutover. That guard is intentionally unchanged.
3. `backup()` makes an online SQLite snapshot while the current application may still write, and checks it with `quick_check`. This is useful for ordinary release recovery; it is not the required final backup after writers have stopped, and it does not perform the requested full `integrity_check`.
4. `recover()` restores the prior Compose configuration and containers. It does not restore `SQLite.db`. `DatabaseMigrationStartup.ApplyAsync()` applies EF migrations automatically when the candidate app starts. Therefore a failed migration can leave the database changed while the old app is restored.
5. Ordinary cutover starts the candidate while Nginx is part of the live stack. It performs Guard and Git activation after starting ingress. The maintenance transaction uses its own ordering and fence.
6. `verify_tooling()` requires host-installed security scripts and Guard units to match the target commit. The ordinary rollout notes do not constitute a separately reviewed maintenance-tool bootstrap. An unreviewed helper cannot safely install itself into the live checkout.

These are independent gates. A helper-only bypass would still stop in `stage()`, and bypassing both would expose production data to a migration without a quiesced final backup or database-aware recovery.

## Release safety contract

Each transition must be persistent, bound to the approved SHAs, audited and fail closed. This table describes the checks used to review the implementation.

| State | Required action and evidence | Failure handling |
| --- | --- | --- |
| Review | Confirm the exact current PROD2 tip, latest successful CI run and attempt plus required jobs, candidate and checkout SHA, clean/trusted Git state, active manifest, mode-0600 environment and approval files, pinned Nginx image, live topology/data volume, installed-tool hashes, and both deployment/Guard locks. Classify the complete before/target diff. | Reject stale or ambiguous CI, any changed gate, combined unrelated critical changes, deletes/renames, symlinks, submodules, or unreviewed paths. |
| Validate on DB clone | Build the exact candidate image. Take a consistent SQLite snapshot of the existing production volume, run full integrity and migration-history checks, and copy it to a private temporary directory. Run candidate startup migrations and health checks against only that clone with no network, no published ports, and no shared production Docker network. Verify the source volume is logically unchanged. | Stop and remove all clone containers/files; retain sanitized audit data. Never mount the production volume in the rehearsal container. |
| Backup | Warn about expected downtime. Stop external ingress first, drain/stop Guard and app/background writers, prove no other container or host process can write the named production volume, then make a final WAL-consistent snapshot. Require root ownership, a mode-0700 directory, mode-0600 files, full integrity, and verified migration history. Keep previous backups and release bundles. | Keep ingress closed and writers stopped if state is uncertain. Do not report a completed backup or continue. |
| Approve SHA | Re-read CI, checkout, Git cleanliness/origin, active manifest, environment/approval bytes and modes, topology, tool hashes, and locks. Show the downtime warning and exact target SHA. Require an interactive confirmation and a second entry of the full target SHA. Re-read all gates immediately after confirmation. | Any difference invalidates the approval and requires a fresh review. Never allow `--check-only`, noninteractive input, environment flags, or generic force options to authorize maintenance. |
| Deploy | Consume a one-use maintenance capability tied to the exact SHA and operation. The release engine independently validates it and the allowed diff. Keep ingress closed while the candidate starts, migrations run, full SQLite integrity/history checks and app health succeed, Guard checks complete, and Git activation is verified. | Before traffic is reopened, stop writers, restore only the verified final SQLite snapshot, verify integrity/history, then start and verify the prior app. If restore cannot be proven, remain offline for intervention. |
| Reopen traffic | This is the irreversible data boundary. Reopen ingress only after every pre-open check succeeds; record the boundary before the first public request can reach the candidate. | If traffic has reopened or writes may have occurred, never automatically restore an older database snapshot. Keep the service stopped or in maintenance mode and require manual intervention/forward repair. |

### Proposed sensitive-file scope

The initial classifier accepts new append-only EF migration source/designer pairs under `DataAccessLayer/Migrations/`, plus edits to existing related model, snapshot and test files listed in `gf3_maintenance.py::RELATED`. Review each migration source together with its designer. Moves, deletions, symlinks, submodules, edits to existing migrations and changes to database startup code require separate review.

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
4. Back up the host-local menu and require SHA-256 `582200f62900359d7e7593ba430c3d35ce10129e47842826bd585bab9b7b995a`. Stage and verify the reviewed Git blob `deploy/security/gfctl-prod2-maintenance-menu.patch` as above. Apply with `patch --batch --forward --fuzz=0 -o "$stage/gfctl" "$backup/gfctl" "$stage/gfctl-prod2-maintenance-menu.patch"`. Run `bash -n "$stage/gfctl"` and verify the result has SHA-256 `b54f28e0e4e4a36c2296e96e069d0f4cb0a179eb18847fda91eb8d88e6759313`. Stop on a mismatch. Install through the existing menu backup and atomic replacement procedure. This adds **[13] Rehearse PROD2 database maintenance on a copy**.

These steps install rehearsal tooling outside the live Git checkout. Establishing a new active tooling baseline is a separate reviewed release operation. The ordinary helper continues to classify deployment scripts as critical and there is no tooling-and-migration exception.

## Operator runbook before tool installation

- Use the existing managed release tools until the exact new tools and their bootstrap record have passed separate operator review.
- Do not edit, copy, or restore the production SQLite database manually. The normal release rollback bundle is not a database-migration rollback guarantee.
- For an unrelated ordinary release, continue to use the installed, reviewed host tooling and its existing runbook. Do not install changed security tooling from this proposal.
- The rehearsal review and install procedure applies only to the clone operation. Live maintenance installation remains unavailable.
- The eventual operator should verify the working database using SQLite `PRAGMA integrity_check`, confirm the expected `__EFMigrationsHistory` rows, and exercise application health before ingress reopens. The final backup should be inspectable as a root-only artifact under a private maintenance bundle. Those commands and paths must be supplied by the reviewed implementation; they are not established by the current host tooling.
- Before traffic reopens, a failed migration may be rolled back only by restoring the verified final snapshot while ingress and every writer remain stopped, then verifying the database and previous app. After ingress reopens or any write may have happened, do not restore the snapshot; stop for manual intervention or forward repair.

## Host compatibility required at installation

The host needs nftables, a local Docker daemon with file-backed volumes, the current managed release layout and access to the host process table. Any unrecognized writer, privileged container with data access, changed fence or changed rollout state blocks maintenance. The protection assumes trusted root and Docker operators. Direct Docker commands, database tools and other privileged automation must share the deployment coordination or stay out of the maintenance window.
