# Schedule generation verification

Pinned source: `DEV2` at `30dcaf549f5ddb59eeb2a57f27b64220bb658cc3`.
The initial checkout matched this SHA and was clean. The verification additions and targeted fixes are uncommitted changes against that source.
Environment used on 2026-10-26: Windows 10.0.19045, win-x64, .NET SDK 10.0.401, runtime 10.0.12.
The existing `TechStacks.md` constraints were retained; no new packages, schema, public contracts or production DI changes were needed.

## Commands

Run from the repository root unless indicated otherwise:

```powershell
dotnet restore GF3.sln
dotnet build GF3.sln --no-restore

# Bounded generation, service/persistence, scoring/display and controller checks.
# The extra category clause deliberately excludes the extended MemberData cases.
dotnet test GF3.Tests/GF3.Tests.csproj --no-build --filter "(FullyQualifiedName~ScheduleGenerationInvariantTests|FullyQualifiedName~ScheduleGenerationScenarioMatrixTests|FullyQualifiedName~ScheduleCrossGraphConflictTests|FullyQualifiedName~ContainerServiceTests|FullyQualifiedName~ScheduleGeneratorScoringTests|FullyQualifiedName~ScheduleGenerationDisplayTests|FullyQualifiedName~GenerationController)&Category!=GenerationStress"

# Optional extended run: 200 fixed seeds, 1000 through 1199.
dotnet test GF3.Tests/GF3.Tests.csproj --no-build --filter "Category=GenerationStress"

# Complete backend regression run without the extended stress matrix.
dotnet test GF3.Tests/GF3.Tests.csproj --no-build --filter "Category!=GenerationStress"

dotnet build tools/AlgorithmStressHarness/AlgorithmStressHarness.csproj
dotnet run --project tools/AlgorithmStressHarness/AlgorithmStressHarness.csproj --no-build -- --help

Push-Location FrontEnd
npm run lint
npm run build
Pop-Location
```

The baseline restore, unqualified solution build and `dotnet test GF3.Tests/GF3.Tests.csproj --no-build` were actually executed before production changes. Later builds/tests used `--disable-build-servers -m:1 -p:UseSharedCompilation=false` to avoid stalled reusable MSBuild workers. While a test host held its DLL, focused builds also used `-p:OutputPath=bin/GenerationVerification/`, `bin/GenerationReduced/` or `bin/VerificationFinal/`. These flags change build execution/output locations, not scheduling behavior. The final solution build in `bin/VerificationFinal/` passed with zero warnings/errors. `dotnet build-server shutdown --msbuild` completed after validation.

TRX evidence was generated with `--logger "trx;LogFileName=<run>.trx" --results-directory "$env:TEMP\GF3.GenerationResults"`. Named runs include `generation-baseline`, `hours-reduced-red`, `integration-red`, `reduced-green`, `benchmark-baseline`, `focused-final`, `stress-baseline`, `stress-final`, and `all-final`. These files contain synthetic tests only and are outside Git. Ordinary CI already excludes `Category=LocalOnly`; stress cases have both `GenerationStress` and `LocalOnly` traits. For local full-suite runs, use the explicit exclusion above; bare `dotnet test` includes stress tests.

## Rule classifications

* **Hard invariant:** dates, positive same-day minutes, slot count/position/status/ownership, enrollment, duplicate positions or employees within one shift, availability intersection, overlapping work, saved external work, positive monthly maximum and work streak. Assigned intervals must remain within the overall shift envelope. Duration totals are calculated in integer minutes; touching endpoints are legal.
* **Optimization quality:** feasible coverage, minimum/desired hours and fairness. The small reference search enumerates only a two-slot synthetic day with a 1,000-state cap; it verifies designated solvable cases, not optimality for arbitrary months.
* **Expected infeasibility:** capacity is below demand, no employee can work, or minimum targets exceed a proven bound. Unfurnished positions and deficits are metrics, not automatic defects. Cases without a feasibility proof remain `PARTIAL` rather than being labelled unsatisfiable.
* **Input/schema error:** malformed availability or shift settings, missing source, or incompatible schema. Offline replay refuses or skips these cases with fixed redacted reason codes; it never repairs data.
* **Performance:** generation-only median/maximum, whole preview duration, replay end-to-end duration and process peak working set. Timing and memory are diagnostics rather than brittle assertions.

Missing availability days retain the current unrestricted convention. `NONE` blocks a day. A required unreadable `INT` blocks assignment; applicable groups and duplicate day entries intersect. Minimum hours are targets. Positive `MaxHoursPerEmpMonth` and `MaxConsecutiveDays` are hard bounds.

`MaxFullPerMonth` and `MaxConsecutiveFull` retain the explicit-full-day exception while minimum hours remain unmet. Since assignment-time unmet minimum cannot always be reconstructed, ambiguous final schedules increment `FullDayOverrideReviews`. Strict controls without the exception are hard checks. A focused test exercises the real override and its zero-minimum control. Desired-hour deficit is independently computed by capped equal-share allocation; the test oracle deliberately leaves `RestPenalty` null because it does not reproduce the engine's internal rest scoring. Existing scoring tests still verify that priority.

The default factory matrix contains 96 named cases, with seeds 11, 29 and 47. Additional tests cover malformed-oracle inputs, cancellation after generation has begun, invalid configuration outcomes, full-day limits/override, duplicate-day and multi-group intersections, 18 reduced partial-window controls, three identical repeats, input permutations and three exact reference cases. A benchmark runs 27 configurations (5/10/20 employees, 28/30/31 days, staffing 1/2/3), three repeats each. Stress cases span 1–20 employees, staffing 1–3, four calendar boundaries, nine availability patterns and several limits; each generator invocation has a 30-second cancellation bound. Offline generation has a two-minute per-case bound and propagates Ctrl+C.

## Read-only local replay

The initial synthetic-only execution had no production-copy database: **NOT RUN — local DB unavailable** at that stage. On 2026-10-26 the user subsequently supplied an explicit local copy and the offline real-data replay was executed. Its anonymized per-case reports, source fingerprints and before/after measurements are retained outside the repository. The fixture verification described below remains synthetic. No production host or remote connection was accessed.

Prepare a consistent backup through an approved operational workflow outside this repository. SQLite's online backup API/CLI `.backup` can produce a standalone snapshot. Copying only the main database while a WAL writer is active can omit committed rows: preserve the matching WAL sidecars until a consistent backup is produced, or copy a cleanly stopped database. Do not combine sidecars from different snapshots. Do not place credentials, database files or backups in this repository, reports or CI artifacts.

Example paths are placeholders, not discovered user files. Create the private report directory beforehand:

```powershell
dotnet run --project tools/AlgorithmStressHarness/AlgorithmStressHarness.csproj -- --db "D:\Private\GF3\snapshot.db" --all-graphs --repeat 3 --output "D:\Private\GF3-Reports\generation-results"
# Alternatively select one graph with --graph <positive-id> instead of --all-graphs.
```

Flags:

| Flag | Contract |
| --- | --- |
| `--db` | Required absolute existing local SQLite file. Relative, network and linked/reparse paths are refused; no fallback or file creation. |
| `--all-graphs` / `--graph` | Exactly one selector; graph ID must be positive. IDs remain internal and never appear in reports. |
| `--repeat` | 1–10, default 3; each repetition opens a fresh read-only context/input. |
| `--output` | Required absolute existing local directory outside Git repositories, separate from the source database directory. Linked output paths/report files are refused. |
| `--help` | Prints instructions without opening a database. |

The four existing positional HTTP arguments remain supported by the original path; offline mode is opt-in. Do not use the legacy HTTP defaults as evidence about production data. The legacy HTTP output behavior is unchanged; the offline report path supplies redaction. The HTTP path was not exercised against a running API in this execution.

The loader explicitly opens `Microsoft.Data.Sqlite` with `Mode=ReadOnly;Pooling=False`, sets/verifies connection-local `PRAGMA query_only`, checks exact migration compatibility and every mapped table/column, and uses no-tracking reads. It never invokes migrations, schema creation/deletion, application startup or a write service method. It composes the concrete repositories and real generator through `ContainerService.GenerateGraphPreviewAsync`, including selected availability and other graphs' saved commitments. Target graph slots are loaded only for comparison.

The generator stopwatch wraps only `ScheduleGenerator.GenerateAsync`; database loading/report serialization are excluded. Whole service-preview timing is recorded separately. Repeated normalized slot tuples are compared internally for determinism. Reports contain no slot tuples or database-derived golden schedules.

| Output | Contents |
| --- | --- |
| `summary.json` | Pinned SHA, mode, UTC time, schema/read-only status, case totals, skipped/failure counts, end-to-end time, peak working set, and anonymized case summaries. |
| `cases.csv` | Sequential `case-001` labels; employee/availability-entry counts; generated/assigned/unfurnished counts, conflicts, coverage/minimum deficits, median/maximum generation milliseconds, stability and violation count. |
| `failures.json` | Only failed/skipped case labels, fixed reason/classification, repeat stability and aggregate invariant codes/counts. |

Each case summary includes calendar/generation settings, generated aggregate metrics, saved-slot comparison metrics, generation/preview durations and repeated-output stability. Historical saved slots can have different staffing positions, so their comparison deliberately does not enforce generated-position count/order. The harness projects the same slot/availability/overlap/monthly-hours/work-streak rules independently of the test assembly. The synthetic fixture compares its aggregate metrics against the test oracle; disagreements fail verification.

The real-data audit extends the projection with strict full-day controls and explicit-override review counts. `Quality` includes optimistic coverage/minimum-deficit lower bounds, raw assigned-hour spread, minimum-target attainment, missing selected availability and relevant saved-commitment counts. Bounds relax full-day/staffing interactions; excess above a bound is an investigation candidate, not proof of avoidability. The combined monthly-demand bound is disabled for nonadjacent/overlapping templates. Raw hour spread alone is not a fairness defect when minimum targets, availability or constraints differ.

`LocalQuality` searches fixed-time employee transfers and two-slot swaps for a constructive minimum-deficit/squared-deficit improvement, preserving coverage and verifying the candidate with the independent hard checks. It emits aggregate scores only, never a schedule or personal intervals. Search is bounded to 20,000 states/one second per case and flags truncation; no witness is not a global optimality proof. An actual witness is checked against the full oracle before being reported.

The offline integration test verifies all database row values and the main-file SHA256 before/after replay, checks three stable repetitions, scans all three reports for synthetic names/contact details, raw IDs, paths and availability payload fields, and verifies safe failure for absent/relative paths, unsupported schema, unsafe output and repeat limits. No UPDATE/DELETE probe is used to test source immutability. Exit codes are 0 for successful invariant-safe replay (including honest partial coverage), 1 for case failures/instability/skips, 2 for refused input/schema/read/output failures, and 130 for cancellation. SQL exceptions are redacted rather than printed verbatim.

## Reproduced defects and measurements

Baseline: solution and harness builds passed; 862 existing backend tests passed in 7m05s. The first bounded matrix/repeat/reference run passed 99 tests. The extended baseline passed 197/200; seeds 1085, 1130 and 1166 violated individual monthly maximum hours. A separate real-generator/two-context integration test failed for parallel writes to different graphs sharing an employee; its sequential control passed.

**Monthly-hour defect:** exact-day options estimated each shift independently. Pair materialization could extend an interval beyond its template boundary and the old acceptance check considered coverage alone. The reduced regression uses two employees, two staffing positions and seed 1085, with max 80 hours and a one-day work streak; adjacent count/staffing controls are retained. The fix checks actual materialized totals before accepting an exact-day improvement, using the existing rollback for an unsafe candidate. It changes no score priority. The reduced 18-case controls and both serial/parallel cross-graph cases subsequently passed (20/20).

**Concurrent-write defect:** both services could load the same empty other-graph snapshot and commit overlapping schedules. Persisted generation now retains its per-graph semaphore and also holds a shared year/month semaphore across fresh graph/employee/commitment loading, generation and transactional persistence. Graph state is re-read after acquisition, retrying if its month changed; locks release on failure/cancellation. Preview/dry-run does not acquire the month lock. Same-graph concurrency, source snapshots, row-level repeat stability, manual-offer IDs/intervals/slot-number reservation, append uniqueness failure, injected failure, mid-generation cancellation and replacement rollback after a database insert failure are covered.

This is same-process coordination. Multiple writer processes/replicas still require a separately approved database-level coordination solution; cross-process protection was not implemented or verified.

The failing stress inputs all have demand greater than the aggregate positive monthly cap, proving complete coverage impossible. Their former coverage totals included illegal individual overtime. Rejecting that overtime can increase the reported coverage gap; this is a safety correction, not a feasible-score improvement claim. Remaining minimum deficits are reported without claiming a proof that every such target is infeasible.

| Seed | Hard violations before/after | Assigned minutes before/after | Coverage gap before/after | Minimum deficit before/after | Generation ms before/after |
| --- | --- | --- | --- | --- | --- |
| 1085 | 2 / 0 | 28,980 / 28,680 | 33,660 / 33,960 | 0 / 0 | 1,456.4 / 1,527.6 |
| 1130 | 3 / 0 | 52,740 / 52,260 | 12,060 / 12,540 | 480 / 540 | 7,054.3 / 7,318.0 |
| 1166 | 2 / 0 | 49,860 / 49,560 | 14,940 / 15,240 | 0 / 0 | 5,196.2 / 5,282.8 |

These are single diagnostic stress executions, not medians. The separate three-repeat benchmark comparison below used the same inputs/machine. All 27 configurations had zero hard violations, conflicts, coverage gap and minimum deficit before/after, with stable normalized output on each side. The largest final/baseline median ratio was 1.243; no benchmark exceeded twice its baseline. Example medians:

| Employees / days / staffing | Baseline median/max ms | Final median/max ms |
| --- | --- | --- |
| 5 / 28 / 1 | 10.935 / 36.630 | 8.230 / 8.474 |
| 10 / 30 / 2 | 23.738 / 26.592 | 19.665 / 19.944 |
| 20 / 31 / 3 | 35.001 / 36.680 | 25.968 / 27.088 |

Process peak working set was approximately 98.6 MB in the baseline benchmark host and 110.4 MB in the final focused benchmark host; these include test-host work and are not a generator allocation comparison. Frontend lint passed with 16 warnings in unchanged files; frontend build passed with SignalR annotation/chunk-size warnings. Browser/E2E, hosted CI and physical devices were not run.

Final verification: **1,014/1,014 backend regressions passed** (7m32s), **200/200 extended stress cases passed** (1m59s), and **172/172 final focused checks passed** (55s). There were no skipped or failed tests in these runs. The 172 focused checks include 96 named factory cases, 40 invariant/oracle/configuration cases, six repeat/reference cases, one 27-configuration benchmark, and 29 existing/extended service, cross-graph, scoring/display and controller checks. The disposable SQLite replay passed again in both the complete and final focused runs, including source row/checksum preservation and redaction/schema/path refusal checks. Tracked and newly created files passed whitespace/diff checks. No external real-data claim is made.

## Reproducing and retaining a regression

The subsequent real-data run reproduced two additional engine issues with reduced synthetic tests:

* Disjoint partial windows could fail pair resolution during exact-day materialization, leaving zero internal hours but full assigned template times in the output. The shared resolver now keeps positive clipped, nonoverlapping intervals when workers have a real gap. Adjacent, overlapping and separated templates, and monthly accounting, have dedicated regressions.
* The final result could admit a safe fixed-time transfer or swap improving minimum-hour deficits or their squared imbalance. A bounded final repair now makes only strict improvements in those existing priorities, retains worked intervals/coverage, and reuses availability, overlap, monthly/work/full-day checks. It updates internal ownership/hour/day counters without recomputing time boundaries. Equal and unequal synthetic minimum targets reproduce the earlier miss.

The repair has at most twice the month's day count in passes, honors cancellation, and preserves the established conflict/coverage/minimum/squared-minimum scoring order. Actual local-database metrics and remaining source-configuration limitations belong in the private replay report rather than source fixtures or Git.

Filter a failing seed, for example `--filter "FullyQualifiedName~ExtendedFixedSeeds_HaveNoHardViolations"` with detailed/TRX output, or temporarily restrict its `MemberData` to that fixed seed for diagnosis. Keep reports restricted to scenario ID, seed, ordinal and aggregate metrics. Reduce employees/positions/windows until the issue has a small deterministic reproducer, add it to the bounded invariant tests, demonstrate red on the pinned engine, then apply the narrow fix and run focused, stress and complete backend checks. Do not weaken the oracle or treat every infeasible target as a defect.

For future before/after runs, use isolated checkouts/build output directories at the recorded baseline and candidate, identical synthetic inputs/seeds and the same SDK/machine. Run `RepresentativeBenchmarks_ThreeRepeatsRemainSafeAndStable` with TRX output for generation-only timings; compare conflict/coverage/minimum metrics before later-priority quality scores and inspect any median slowdown over 2x. For local database replay use the same consistent read-only backup and separate private report directories, comparing only redacted aggregates. Never connect to live production for benchmarking or upload its database/report payloads into CI.

## Changed files and scope

* `BusinessLogicLayer/Generators/ScheduleGenerator.cs`: reject exact-day candidates with materialized monthly overtime.
* `BusinessLogicLayer/Services/ContainerService.cs`: same-month persisted-generation coordination and fresh reload.
* `GF3.Tests/Infrastructure/ScheduleGenerationScenarioFactory.cs`, `ScheduleGenerationOracle.cs`: deterministic inputs and independent checks/metrics.
* `GF3.Tests/ScheduleGenerationInvariantTests.cs`, `ScheduleGenerationScenarioMatrixTests.cs`: oracle controls, bounded/stress/reference/cancellation/benchmark cases.
* `GF3.Tests/ContainerServiceTests.cs`, `ScheduleCrossGraphConflictTests.cs`, `WebApiControllerAndMiddlewareTests.cs`: real SQLite/service/controller/persistence/race/replay regressions.
* `tools/AlgorithmStressHarness/Program.cs`, `AlgorithmStressHarness.csproj`, `SqliteReplaySource.cs`, `ReplayReportWriter.cs`: optional safe offline replay, schema/path safeguards and aggregate reporting.
* `docs/schedule-generation-verification.md`: commands, operational safeguards and execution evidence.

No source-data fixtures, public API/schema changes, migrations, production DI changes, dependency additions or frontend edits were introduced. The default focused command's added stress exclusion and build-output/worker flags are execution adjustments. The subsequent explicitly supplied local-copy replay is documented in private artifacts outside Git; the historical synthetic counts above describe the earlier verification stage.

## 2026-10-27 priority hardening execution

Source: clean `DEV2` at `a9e79fffab1d5d1adc298ef1f32a66cc429d53cf`; SDK 10.0.401, .NET/ASP.NET runtime 10.0.12, Windows x64. The attached Plan (49) is authoritative. The existing TechStacks.md is the technical inventory (no singular TechStack.md present).

Baseline reproduction before production edits:

- Boundary/November fixtures: 7 passed, 2 failed (15s). Opening-only early/late anchors already pass at this SHA; that part of the analysis hypothesis is disproven. External free-window clamping is reproduced: positive 09-15 with external 10-22 emits no assignment instead of 10-18; positive 16-23 with external 09-21 emits none instead of 13-21. Three-repeat November fixtures include no/mixed/all priorities, zero and extreme targets, staffing four and monthly caps. Impossible minima return partial results without an exception.
- Budget witness: failed, 65 assignments versus independently attainable 84 (21 weekdays times four exact 8h assignments), all-priority November, 20 employees, staffing four, 09-22 envelope. This confirms premature construction termination.
- User-specific UI/server error: UNCONFIRMED; no actual failing HTTP response or console trace supplied.
- TRX and runtime/hash output are retained in private temporary verification directories outside Git. A parallel compilation collided with the baseline host's loaded DLL; subsequent red runs use a distinct output directory. No product conclusion is drawn from that file-lock build failure.

Results below are appended from executed checks; infeasible demand is distinct from hard violations, and bounded quality evidence is not a global optimality proof.

Original focused baseline (including selected stress theories): 582/582 passed, zero skips, 5m36s, before harness/production changes. Harness build after Step 2: passed, zero warnings/errors.

Steps 2-5 progressive checks:

- Harness compilation: passed. Existing synthetic disk replay test passed (39s), exercising legacy plus app stored/none/first/first-two/all, missing settings defaults, source snapshots/SHA-256, quick_check, repeat determinism, redaction and invalid paths/schema/flag combinations. Legacy enrollment order is retained; variant selection ranks IDs independently without reordering the input.
- Interval regressions: 14/14 passed (1s); both external-window red cases are green, existing handovers remain correct, 1/6/8/10/12h overlapping-template envelopes and real shift gaps are checked.
- Construction/cancellation/November regressions: 9/9 passed (15s). The formerly 65-shift budget witness now reaches all 84 feasible assignments. Required reservation/hint/fill work is independent of the 50k optional improvement budget. Residual filling scans vacant positions, honors cancellation, and terminates on the position bound or a pass with no additions.
- Atomic replacement/exchange regression selection: 91/91 passed (1m04s). Proposed states are separate copies; rejected/half-valid exchanges never mutate the accepted list. Quality ordering is retained, and whole-state validation precedes committing an improving move and final materialization. The engine continues to validate rejected-move and large search fixtures with repeat hashes and the independent oracle.

Steps 6-9 evidence:

- Tiny independent enumeration: all three bounded discrete reference configurations attained the best lexicographic deficit/coverage/normalized-fairness vector; the existing scarce-worker constructive witness also passed (4/4). State-cap exhaustion is explicitly labeled INCONCLUSIVE.
- Service/API: initial 27 checks passed (26s); final fresh/recreate and HTTP selection passed 17/17 (22s). Disposable SQLite fixtures cover durations 1/6/8/10/12, both weekend states, no/one/two/all priorities, missing settings, preview without writes, save/reopen, delete/recreate, partial infeasible generation, repeat/idempotence and concurrent/cross-graph generation. Fresh and persisted preview tuples agree. Existing rollback/cancellation/append checks remain enabled.
- Frontend regressions reproduced stale success/error responses after changing selected availability and untranslated network TypeError feedback (5 red assertions). The minimal editor fix invalidates generation tokens when availability changes and uses the existing localized fallback for network errors. Focused editor/query/priority-label selection: 35/35 passed; TypeScript build passed. Server 400/404/500, network, cancellation, retry and priority preservation are tested in EN/PL/UK. Save-retry pending state is covered with the real local mutation client.
- Chromium mocked browser scenarios: 2/2 passed (24.9s). The fresh graph case deletes an obsolete synthetic graph, selects availability, auto-enrolls, previews zero/one/two priorities, changes settings, observes a 500 error and retries, saves/reopens flags/intervals, then switches availability. Phone settings remain read-only. This is browser/UI evidence with mocked APIs; backend execution is verified separately above.
- Ordinary priority matrix: 46 fixed rows cover every two-factor combination of 5 calendars, 5 roster sizes, 4 staffing levels, 5 durations, both weekend states, 4 priority selections and 9 availability patterns. A separate test exhaustively asserts pair coverage. Each row checks three repeats; priority rows also check canonical employee/group permutations. The 12-person/four-staffing November configuration has separate fixtures. Fixed priority stress seeds 1000..1199 are opt-in GenerationStress/LocalOnly and check three repeats with 30-second cancellation bounds. Existing legacy stress seeds also now compare three repeats.
- Independent oracle metric regression reproduced: overlapping 09-16/15-22 templates counted 25,200 rather than 23,400 missing minutes for April. Priority-mode demand now counts the union once; legacy-mode metric semantics are retained. That red regression and missing-member/duplicate-enrollment controls passed (2/2).

Additional quality/performance regressions and repairs (executed after the initial matrix):

- A real old-to-new run used the retained baseline generator assembly with the current read-only harness, not an AI report. All 32 no-priority graphs had identical ordered output SHA-256. Initial position-scanning construction reduced coverage in the priority November case from 87,330 to 86,220 minutes; that intermediate regression was investigated rather than declared acceptable infeasibility.
- The independent bounded checker initially used the legacy squared-deficit objective; priority-mode checking now reports the complete canonical priority/coverage/regular/normalized/fairness/rest/vacancy vector, with canonical summation order and aggregate witness vectors. A strict improving regular pair was confirmed after 10,518 states. Three fully synthetic mixed-availability seeds 11/29/47 reproduced strict improving pairs (3 red tests). Filtering non-improving ownership changes before expensive validation retained the 50k validation quota and made these checks green. Canonical fixed-pair scoring avoids whole-state allocation; accepted moves still undergo complete validation and full-vector comparison.
- Atomic vacancy handover is needed when a worker must relinquish one interval to another regular worker to cover an empty position. The temporary two-assignment proposal is accepted only as a strictly improving, fully valid whole state. Fixed seed 29 now covers at least 74,580 minutes versus the earlier 74,520; seed 47 covers at least 71,580 versus 71,460. These constructive witnesses are retained as assertions.
- The real priority case after the handover repair covers 87,750 minutes (7 hours above the pinned engine), has 5,850 gap minutes versus 6,270, and 41,850 deficit minutes versus 42,270. Priority deficit stays 3,840 minutes, exactly its aggregate optimistic lower bound. Its bounded fixed-time search completed 17,562 states without finding another improvement. These results are bounded quality evidence, not global optimality.
- Stress seed 1139 exposed a separate runtime limit in the optional full legacy guidance search. Intermediate pre-fix stress runs reproduced 30-second cancellation and were stopped after the failure was isolated; they are not counted as successful complete stress runs. No legacy source was modified. Priority mode now omits optional legacy hints when the deterministic positions-times-regular-roster-pairs estimate exceeds that phase's 50k quota; required filling still runs. This avoids a timing-dependent fallback. The named seed plus construction/cancellation/reference/quality regressions subsequently passed 9/9 (47s).
- Optimized quality and 48 benchmark configurations passed 54/54 (1m38s). Final source regression and complete suites are recorded below.

Read-only source handling: the executor's supplied directory contains a WAL database, with 33 schedules rather than the plan snapshot's 34 (333 enrollments and 3,264 saved slots; 35 availability groups and 5,817 availability days). Five graphs select availability; one has priorities, using stored 8h/weekends-off settings. This is a snapshot mismatch, not a fabricated missing case. Immutable SQLite ignores unapplied WAL, so the harness fingerprints the original DB/WAL/SHM, copies DB/WAL to a private temporary directory, checkpoints only that disposable copy, then opens the copy with Mode=ReadOnly, Pooling=False, immutable=1, no tracking and query_only. Original files are never opened by SQLite. Original integrity/fingerprints are compared before/after; private copies are removed on disposal. Synthetic WAL replay and source-guard tests cover this path. Output remains separate from the repository and source.

Frontend final checks: npm ci completed using the unchanged lockfile (its audit reported 18 dependency vulnerabilities; no out-of-scope dependency upgrades were performed); lint passed with 16 warnings in unchanged files; 110 test files / 997 tests passed (111.43s); TypeScript/Vite production build passed with existing SignalR annotation and chunk-size warnings. All eight configured priority browser scenarios passed in desktop/mobile Chromium and WebKit (1.5m); APIs are mocked and all fixture data is synthetic. This does not certify physical devices or production.

Final real-data evidence: 33/33 graphs in legacy mode and 33/33 in application stored mode, each with three repeats; all five selected-availability graphs in none/first/first-two/all variants, also with three repeats. This is 258 executions of the final candidate engine/harness, plus 99 comparison executions using the retained pinned generator assembly (357 total executions across 119 case-runs, not 119 distinct graphs). All had zero hard violations, zero runtime failures/skips/timeouts, stable ordered output hashes and unchanged source fingerprints. All 32 no-priority graphs match both the baseline assembly and the legacy engine exactly in ordered normalized tuples. The supplied database has 33 schedules, so the plan's unavailable 34th schedule cannot be certified.

The final independent source audit confirmed DB/WAL/SHM SHA-256, lengths and nanosecond modification timestamps unchanged, identical table counts and quick_check=ok. JSON/CSV reports were scanned against source names/contacts without printing those values; no personal values, raw roster identifiers/interval arrays/calendars or source path were found. Reports, TRX, selection metadata and fingerprints remain in private temporary verification directories outside Git. No production server was contacted.

The retained baseline assembly also passed all 48 final benchmark configurations, enabling measured median/max/allocation comparison rather than comparison to generated text. Final candidate comparison is recorded after the complete test run. Concurrent-run timings are observational and do not establish a universal runtime bound.

Final verification:

- Solution restore: passed (the sandboxed invocation exited without diagnostics; the permitted elevated rerun completed normally). Final solution build: passed, zero warnings/errors. Harness build and CLI help: passed, zero warnings/errors; snapshot cleanup verifies its resolved path stays under the private temporary root before recursive removal.
- Final focused backend selection: 492/492 passed, zero skips/failures, 10m22s. This includes all newly retained quality, timeout, WAL, service/API and oracle regressions. The named former timeout seed passed in 9.55s total for three generations.
- Forty-eight benchmark configurations (5/10/20 workers, staffing 1/2/3/4, months 2/4/7/11) were run three times using both the retained pinned assembly and the final test code. Every final median was below its measured pinned-assembly median in these runs; the largest final/baseline median ratio was 0.878. Example n=20/staffing4/July: final metrics are retained in the private comparison. No 2x latency regression or benchmark time-bound failure occurred. The largest observed allocation ratio was 1.089 in n=5/staffing4/November (164,026,125 to 178,499,243 total allocated bytes); the extra constructive/atomic work is a measured cost, not omitted from reporting. These are process allocation deltas and concurrent-run timings, not universal performance guarantees.
- The November real-data generation median/max changed from 8,312/11,486ms to 4,866/5,393ms in the recorded three-repeat comparisons. Whole-process replay peak working set was 112,787,456 versus 116,178,944 bytes (about 3% higher), including source snapshot/report work, not generator-only memory.

Current change scope (17 files):

- BusinessLogicLayer/Generators/PriorityScheduleGenerator.cs and PriorityScheduleRules.cs: bounded phases, clamped priority windows, conditional optional hints, atomic improvements/vacancy handovers and fixed-pair scoring.
- tools/AlgorithmStressHarness/SqliteReplaySource.cs and ReplayReportWriter.cs: actual application routing, in-memory variants, independent priority checks/quality, safe WAL snapshots, integrity/redaction and truthful binary/checkout provenance.
- FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx and ContainerGraphEditPage.generatedManualShifts.test.tsx: stale availability responses and localized network failure feedback/regressions.
- FrontEnd/src/entities/containers/api/queries.test.tsx; FrontEnd/src/entities/containers/ui/ContainerGraphDetailsFields.priority.test.tsx; FrontEnd/e2e/manager-priority.spec.ts: save retry, localization and fresh browser workflow coverage.
- GF3.Tests/PriorityScheduleGeneratorTests.cs; Infrastructure/ScheduleGenerationOracle.cs; ScheduleGenerationInvariantTests.cs; ScheduleGenerationScenarioMatrixTests.cs: deterministic fixtures, independent reference/oracles, pairwise/stress/budget/quality/timeout tests.
- GF3.Tests/ContainerServiceTests.cs; ScheduleCrossGraphConflictTests.cs; PriorityGenerationSettingsControllerTests.cs: preview/save/recreate/concurrency/API/WAL/source guard tests.
- docs/schedule-generation-verification.md: this appended execution evidence. Legacy generator, migrations, API contracts, production settings/configuration, package manifests/lockfile and source database are unchanged.

Necessary execution adjustments: use existing TechStacks.md as the technical inventory; verify 33 available source graphs rather than inventing the missing 34th; preserve the already-working opening-only anchors and fix the reproduced external-window omission; recover WAL only in disposable private snapshots; use deterministic optional-hint work estimation and atomic vacancy handover following reproduced timeout/coverage witnesses. No replacement engine, new production configuration, package or schema was introduced.

Final ordinary backend suite: **1,339/1,339 passed**, zero failures/skips (18m52s). Final opt-in stress suite: **400/400 passed**, zero failures/skips/timeouts (18m36s): 200 legacy seeds and 200 priority seeds, each with three deterministic generations and independent hard-invariant checks (1,200 stress generations). The stress runner used an isolated copy of the final test output and serialized test collections to avoid cross-class timing contention. The final focused selection passed 492/492 as recorded above. No tests were deleted, disabled or weakened to obtain these results. The earlier timeout and quality failures are retained in private red-run evidence and described above; they do not remain in the final suites.

Final status: all available planned implementation/validation work is complete. Remaining evidence limits are the unavailable 34th graph from the plan's different snapshot, the original user-specific HTTP/UI error without an actual failing response/trace (UNCONFIRMED), and lack of physical-device/production/hosted-CI execution. Browser tests use mocked APIs; real backend/API/service behavior is covered separately by the disposable SQLite integration suite. No global-optimum claim is made for arbitrary large schedules.

## 2026-10-27: priority availability policy and early/late reorientation (Plan 51)

Implementation baseline: `DEV2@603735907c2ccf2c5931c98d956166e1f080a421`. The existing .NET 10 / EF Core 10 / SQLite and React / Vite stack is retained. No changes to the legacy `ScheduleGenerator.cs`, production DI, employee enrollment, published availability, historical migrations, or dependency manifests.

The early-start reservation tie-break previously locked priority intervals before ordinary workers could use the residual hours. The new bounded stage tests alternative priority anchors by rebuilding only that day's ordinary assignments and comparing the entire valid month using the existing seven-component lexicographic objective. Only strict improvements commit. The two-pass, 64-trial and 20,000-validation limits include day refill and full trial validation; rejected or canceled trials restore the accepted state. Aggregate Activity tags expose evaluation/trial/accepted-change counts without employee data. Before full trial validation, an optimistic quality precheck skips equal or worse proposals; every committed proposal still passes whole-state validation and the final canonical comparison.

Migration `20261027220000_AddPriorityRespectAvailability` adds only `priority_generation_settings.respect_availability`, required with default true. GET returns `shiftHours`, `weekendsOff`, `respectAvailability`. PUT accepts the new nullable optional field; omission/null retains the stored value, including false. The parameterized singleton UPSERT retains authorization and existing required-field validation. Downgrade removes only the new column.

Settings reuses the existing rounded switch, server-confirmed cache flow and read-only phone behavior, with EN/PL/UK labels and weekday/weekend precedence text. All three handlers submit all three fields; an immediate save guard prevents concurrent writes. Ignoring priority source availability permits exact-duration weekday work within actual contiguous templates, while external saved commitments and ordinary availability remain hard constraints. Respect=true retains existing ordinary full-day override behavior.

The supplied Windows source differs from the planning attachment: its consistent WAL snapshot contains **32 schedules, 321 enrollments, 35 availability groups, 5,817 availability-day records, four selected-availability schedules and zero stored priority enrollments**. The unavailable two schedules and fifth selected-availability schedule cannot be reconstructed or claimed tested. Every available schedule is included. Source main/WAL/SHM fingerprints were verified unchanged while copying; recovery/checkpoint and EF migration operate only on a separate private disposable snapshot outside the repository and source directory. Initial recovered-source `quick_check=ok`.

Offline replay retains existing commands and adds `--priority-availability stored|on|off`, default stored. Overrides require `--engine app`; they apply only in memory through a harness-local adapter. The stored path uses the real production wrapper. Independent checks, capacity references and quality vectors carry the effective policy and protect external commitments. Reports contain pseudonymous case labels, aggregates, hashes, early/late anchor counts and bounded-phase diagnostics; no employee IDs, contacts, raw availability arrays or source path are retained. PARTIAL remains distinct from hard-invariant failure.

Control comparisons retain the exact pre-edit generator assembly (SHA-256 `2DAE75525F0D82281094BEF685B797E4392493C54CD4C848D307918BA2AA465E`). Both controls use the same updated read-only reporting harness and DAL schema adapter so the identical migrated private snapshot is readable; the control BusinessLogicLayer assembly is unchanged. Synthetic comparisons use existing test fixtures and the independent oracle, with warmup and three measured repetitions. Override-off is a new policy and is reported separately from same-policy control comparisons.

Progressive verification: 21 initial policy/anchor/API tests, five focused migration/service/witness checks, ten bounded policy/envelope cases, the exhausted-budget case and the expanded synthetic replay integration passed. Two additional conflicting-group/sentinel cases passed. The first isolated replay-test run failed because its repository discovery assumed test output lived inside the checkout; a current-directory fallback supports the plan's isolated output requirement. No scheduling assertion was weakened.

Frontend: `npm ci`, lint (zero errors, 16 existing warnings), all **1,002 Vitest tests** and TypeScript/Vite production build passed. npm ci reported 18 advisories in the pinned dependency tree; packages were not upgraded. Mocked browser workflow passed **8/8** in the standard four projects/two workers after waiting for settings GET-refetch completion between writes. Earlier WebKit navigation errors were retained as diagnostic evidence; an isolated WebKit run and a full single-worker run also passed. These mocked tests do not prove real scheduling, physical iPhone/PWA, hosted CI or production behavior; the disposable SQLite service tests provide separate backend evidence.

Exact modified/new files:

- `BusinessLogicLayer/Generators/PriorityAwareScheduleGenerator.cs`
- `BusinessLogicLayer/Generators/PriorityScheduleGenerator.cs`
- `BusinessLogicLayer/Generators/PriorityScheduleRules.cs`
- `DataAccessLayer/Models/PriorityGenerationSettingsModel.cs`
- `DataAccessLayer/Models/DataBaseContext/AppDbContext.cs`
- `DataAccessLayer/Migrations/20261027220000_AddPriorityRespectAvailability.cs`
- `DataAccessLayer/Migrations/20261027220000_AddPriorityRespectAvailability.Designer.cs`
- `DataAccessLayer/Migrations/AppDbContextModelSnapshot.cs`
- `GF3.WebApi/Contracts/Priority/PriorityGenerationSettingsDto.cs`
- `GF3.WebApi/Contracts/Priority/UpdatePriorityGenerationSettingsRequest.cs`
- `GF3.WebApi/Controllers/PriorityGenerationSettingsController.cs`
- `GF3.Tests/PriorityScheduleGeneratorTests.cs`
- `GF3.Tests/Infrastructure/ScheduleGenerationOracle.cs`
- `GF3.Tests/PriorityGenerationSettingsControllerTests.cs`
- `GF3.Tests/ContainerServiceTests.cs`
- `FrontEnd/src/entities/priority-generation/model/types.ts`
- `FrontEnd/src/pages/manager-settings/ui/ManagerSettingsPage.tsx`
- `FrontEnd/src/pages/manager-settings/ui/ManagerSettingsPage.test.tsx`
- `FrontEnd/src/shared/i18n/pl.json`
- `FrontEnd/src/shared/i18n/uk.json`
- `FrontEnd/e2e/manager-priority.spec.ts`
- `tools/AlgorithmStressHarness/SqliteReplaySource.cs`
- `tools/AlgorithmStressHarness/ReplayReportWriter.cs`
- `docs/schedule-generation-verification.md`

Necessary implementation adjustment: the new day rebuild reopened ordinary fairness exchanges in three existing seed regressions (11, 29, 47). Those exact assertions passed on the pinned control DLL (3/3), initially failed on the candidate, and remain unchanged. The existing regular improvement loop is now reused once after accepted anchor changes, with at most six additional passes and the SAME cumulative 50,000 optional-validation quota; it never resets that quota or moves locked priority work. This restores the old fairness regressions (13/13 focused anchor/budget/quality checks passed). The trial itself still changes only its selected day. Final backend counts, same-policy control measurements and final source-integrity evidence are recorded below. A bounded non-improvement never proves global optimality for arbitrary months.

Measured final comparison (same frozen migrated snapshot, three repetitions):

- All 32 available graphs: legacy, stored pinned control and stored candidate each passed 32/32 cases with stable hashes, zero hard violations and exact normalized parity. The stored source has no priority enrollments, so this validates the no-priority path rather than priority quality.
- Four selected-availability graphs × none/first/first-two/all × on/off: **32/32 candidate policy cases passed**, each repeated three times. Their 16 same-policy control comparisons contain **three strict improvements, nine equal objective vectors and four exact no-priority parity cases**, zero quality regressions. Together with stored and selected controls this final replay executes **432 generations** (144 case reports × three).
- The three improved first-two cases reduce coverage gaps by **1,680, 240 and 480 minutes**, respectively (2,400 total); priority minimum deficits are unchanged. Ordinary deficits decrease by 1,680 and 240 minutes in the first two; the third preserves ordinary deficit while improving higher-order coverage. Later vector components may rise when a higher-priority component improves, which is allowed by the canonical lexicographic ordering.
- Across the 16 on-policy cases there are **215 early and 72 late priority intervals**, with ten accepted anchor changes. Off-policy cases have **307 early and 32 late intervals**, with 32 accepted changes. These counts describe outcomes rather than an optimization target. All-priority variants select 3, 5, 5 and 6 people in the four anonymous rosters. Both none variants select zero, first selects one and first-two selects two; no extra roster enrollment is persisted.
- Every selected-policy case is honestly **PARTIAL** under the independent coverage/minimum checks; none is a hard-invariant failure. Maximum measured reorientation validations are **572 (on)** and **2,663 (off)**, within 20,000; all trial counts remain within 64. All repeat hashes are stable.
- Same-policy replay median latency ratio is **1.029×**, maximum **1.393×**; none exceeds the 2× investigation threshold. Stored-run peak working set is **116,236,288 bytes control** and **115,507,200 bytes candidate** (about 0.63% lower); this is whole-process working set, not algorithm-only memory.
- Synthetic comparison covers the existing **48 count/staffing/month configurations plus two early/late witnesses**, with both policies on the candidate (98 cases), independent oracle checks and three measured repetitions. After identical JIT warmup, same-policy quality has one strict improvement (the late witness), 49 equal vectors and zero regressions. Median per-case latency ratio is **1.099×**, maximum **1.351×**, median allocated-byte ratio **1.180×**. Median-of-case medians is **166.145ms control / 181.205ms candidate**. The initial one-warmup runs showed >2× first-case outliers, triggered investigation and are retained; identical additional warmups on both versions resolve them without changing thresholds or scored generations.
- The late witness changes priority 09:00–17:00 to **13:00–21:00** and fills the ordinary 09:00–13:00 interval, reducing gap by **240 minutes** and satisfying the full canonical comparison. The reverse witness retains **09:00–17:00** priority plus ordinary 17:00–21:00. Same-coverage unequal-target fairness witness also passes.
- Final candidate generator assembly SHA-256: `5DE04294D1C9E09736FEBF7A8E4570B0AEF6811C74C4E917F5D292013630A159`. The pinned control hash remains unchanged.

Final source integrity: all original main/WAL/SHM **SHA-256 hashes and sizes match the initial fingerprints**, and independent recovered-source quick_check is **ok before and after**. SQLite never connected to the original source; only verified private main/WAL copies were recovered/migrated. Both owned private SQLite snapshot directories were removed after validating their absolute paths under the fixed temporary root. Only anonymized reports, aggregate fingerprints, test logs and synthetic benchmark evidence remain outside Git. An automatic approval-review usage-limit interruption initially prevented this final command; after the user resumed execution it ran successfully.

Validation correction: the first complete ordinary suite reported **1,367 passed / one failed** because `ProductionStaticPipeline_ServesWorkerAsRevalidatedJavaScript` computes its webroot using a fixed four-parent path. The shallow isolated `bin/Plan51Final/` layout caused a 404. The final build uses `bin/Plan51Final/net10.0/`, preserving the conventional depth, and reruns the test/API selection and full ordinary suite. The static-file test and production behavior are unchanged. Two strengthened API checks (actual concurrent SQLite writers and exactly three response fields) passed separately.

The existing 200 priority stress seeds (`1000..1199`) and 200 legacy seeds remain unchanged. Six additional representative policy-benchmark cases add 18 bounded measured generations (three per case); the stress suite itself is not doubled. Synthetic control/candidate policy comparisons run separately and are included in the measured evidence above.

Final stress suite: **400/400 passed**, zero failed, skipped, canceled or timed-out cases (**15m45s**). This includes the unchanged 200 priority seeds and 200 legacy seeds, each with three repeated generations and independent hard-invariant checks (1,200 stress generations). Test collections run sequentially within the runner to avoid the historical timing-contention issue; no stress assertions or time limits were weakened. The complete ordinary suite is recorded separately from the correctly nested isolated output.

Final ordinary suite: **1,368/1,368 passed**, zero failures/skips (**17m14s**), including the unchanged service-worker static pipeline test from the correctly nested isolated output. The specified focused classes account for **557 passed cases** across the final ordinary/stress TRX results; the earlier standalone focused run had 552 passes/three fairness failures before the documented regular-closure fix, and all those assertions pass in the final suites. The final post-pruning regression selection passed **15/15**, and the static-path/API selection passed **9/9**. Combined backend result: **1,768/1,768**, zero failures/skips.

Final build/quality gates: solution restore and build, harness build, frontend npm ci/lint/test/build, and standard four-project mocked browser workflow all passed. `git diff --check` passed. The exact 24-file change set matches the list above. A final post-test read confirms the original database, WAL and SHM still match every initial SHA-256/size fingerprint. No commits, production migrations, dependency upgrades or source database writes were performed.

Residual limitations: the supplied source has 32 graphs/four selected-availability graphs rather than the plan attachment's 34/five; those unavailable cases remain unverified. Stored priority enrollment is zero, so priority policy results come from the explicit in-memory roster variants, reported separately. PARTIAL schedules do not imply hard-rule violations. Bounded search does not guarantee a global optimum, and mocked browser tests do not establish physical-device, hosted-CI or production behavior. No implementation/test failures remain in the final executed suites.
