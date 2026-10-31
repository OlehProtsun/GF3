# Comprehensive scheduling optimization and verification

Audit date: 2026-10-28. Both engines were compared with the actual uncommitted implementation present when this task began. This report is separate from the earlier Priority-only audit.

## A. Executive summary

Legacy now rejects incompletely materialized exact-day plans, respects real gaps between shift templates, canonically orders enrollment input, fills feasible remaining monthly capacity, and extends fixed-interval ownership repair through the established desired-hours and rest criteria. Minimum-hour repair completes before secondary polishing. A strictly bounded small-calendar search handles demonstrated multi-day traps.

Priority retains the existing objective and feasibility rules. Equivalent lane labels no longer consume the whole reorientation trial quota. Cached whole-month projections and a bounded one-pass shortlist rank two complete restarts; successful restart seeds accumulate. Original canonical seeds remain in the shortlist because shallow results do not prove dominance. A final ordinary pair closure uses the remaining original reorientation quota.

The normal production reconstruction now reproduces the previously offline-only vector `[120,4260,17160,0.5823153000705181,7.2078685614669435,177,11]` with 18 early / 3 late Priority shifts under both availability policies. This covers 330 additional employee-minutes without worsening Priority or ordinary minimum deficits. These measurements do not prove global optimality.

## B. Legacy generator audit

### Actual policy and stages

The complete lexicographic vector is:

`[conflict days, template coverage gap minutes, minimum deficit hours, squared minimum deficit hours, desired deficit hours, squared desired deficit hours, generated off-run penalty, unfilled positions, overlap days]`.

A conflict day includes any demand gap or employee overlap. It is not synonymous with a hard violation. Coverage sums the requirements of the individual templates; overlapping-template demand therefore differs from the Priority union-coverage metric. A minimum is a target; assignment may exceed it up to the hard monthly cap.

Desired hours start at each minimum clipped to attainable availability/capacity. Remaining template demand is distributed equally among employees with headroom, repeating when a capacity saturates. Rest penalizes generated days off inside consecutive nonworking runs: for `a` available and `b` blocked days, `L=a+b`, the penalty is `a*L + max(0,L-4)/2`; wholly blocked runs contribute zero. Here availability means a normalized nonempty declared window, not proof that a shift fits it. This differs from Priority's working-run penalty.

The audit inspected availability normalization, scarcity/need/round-robin candidate scoring, initial assignment, paired interval prediction/materialization, unconditional vacant-position fill, explicit-full-day/minimum repair, exact-day dynamic programming, conflict pair/window reconstruction, fairness-window reconstruction, alternative monthly attempts, and terminal fixed-interval ownership repair. Existing exact-day states estimate employees independently; actual paired boundaries still need checking after materialization. Window searches and greedy assignments remain heuristics.

### Confirmed defects and corrections

- A separated-template pair could bridge the real non-operating gap. Each member now clips to its own template when the templates are separated. The minimal regression uses `09:00–13:00` and `15:00–21:00` and verifies exactly 600 worked minutes with no work in the gap.
- Exact-day materialization could fail while retaining assigned template intervals with zero internal hours. Those apparent hours escaped the monthly-cap check and could intersect unavailable/external work. Acceptance now requires positive, availability-contained intervals whose clock duration equals stored hours, plus the existing monthly-cap check. An independently generated synthetic seed reproduces the failure on frozen E69; the three failing real-data variations become valid.
- Enumeration order affected the schedule despite unchanged enrollment `DisplayOrder`. The roster now sorts by display order and employee ID before normalization. Duplicate enrollments still retain their greatest minimum target. ID renaming is a separate experiment, not the same determinism contract.
- Whole-interval additions missed useful capacity smaller than a template. The terminal fill uses actual available segments and remaining monthly minutes, preserving staffing, ownership, overlap, day and full-day rules. Splitting a straddling interval only fills a real vacant counterpart when strict full-day limits permit; it adds no hours.
- The old fixed-interval repair ignored desired hours and rest after minimum ties. It now completes the original minimum-first repair before considering all remaining criteria. Mixing rest changes into the initial repair was rejected after real replay exposed worse minimum outcomes.
- Single moves cannot resolve every capacity exchange across days. Only for at most four employees, one staffing position, two nonoverlapping templates and at most three active dates, a 20,000-state/cancellation bound explores the small calendar component. The original state is restored on cancellation or absence of a strictly better complete vector. Normal monthly rosters do not enter this search. This is not an unbounded solver or a claim of optimality outside the enumerated interval space.

All changes preserve the existing objective. In particular, fewer conflict days can legitimately win despite a larger total coverage gap. The retained tiny seed 5 demonstrates this policy conflict; it was not replaced by a weighted sum or a coverage-first policy.

## C. Priority generator audit

The unchanged complete vector is:

`[Priority minimum deficit minutes, union coverage gap minutes, ordinary minimum deficit minutes, squared normalized deficits, ordinary capacity-utilization penalty, summed squared longest working runs, vacant positions]`.

The normalized term is the sum of squared `deficit / max(1,min(target,potential))`; the ordinary utilization term is the sum of squared `worked/potential`. Potential is availability/external-window capacity clipped by the monthly cap, with Priority using eligible exact-duration days. It is not a proof of combined calendar feasibility. These terms account for differing targets/windows rather than demanding identical hours. Priority intervals retain their exact configured duration; original availability overlap, operating union, external commitments, monthly cap, staffing, positions, overlap and day/full-day rules remain feasibility conditions. Existing explicit-full-day exceptions while targets are unmet are preserved. Availability OFF retains the current weekday-only Priority contract even when WeekendsOff is false; availability ON with WeekendsOff false can use weekends.

The previous two monthly restarts selected only the earliest alternative anchors independently. Meanwhile equivalent vacant lanes consumed reorientation trials, and single/two-day rebuilding could not undo whole-month capacity commitments. The new synthetic late-date regression demonstrates 240 avoidable ordinary deficit minutes on the frozen baseline and zero afterward.

The retained bounds are 64 single-anchor trials / 20,000 reorientation evaluations, 16 coordinated trials / 10,000 coordinated evaluations, six ordinary improvement passes per invocation under a cumulative 50,000-evaluation construction quota, and two fully optimized monthly restarts per construction. Ranking adds at most 32 cached zero-pass projections and eight one-pass refinements for a single-Priority month. It neither rebuilds Legacy guidance for each probe nor recursively restarts those children. Full acceptance always compares all seven criteria and validates feasibility. Final ordinary pair closure shares the remaining 20,000 reorientation quota.

The reconstruction selected canonical initial seeds on dates 3 and 10, carrying the first accepted seed into the second restart. Root reorientation pruned 318 equivalent lane trials. An instrumented reconstruction counted 28,077 root candidate evaluations, 56,895 ranking evaluations and 47,863 complete-restart evaluations. These are separate phase totals, not one unchanged global budget. Instrumentation is checked against plain output and excluded from timed normal generations.

Rejected experiments included projection-only ranking, discarding the original seeds, independent rather than accumulated restart constraints, and cutting off Legacy guidance phases. Shallow estimates ranked dates 17/19/18 ahead of date 3; their rank did not predict the best fully optimized month. The retained strategy uses shallow ranking only to order bounded exploration. A further isolated experiment reran ordinary repair after accepted restarts using the remaining original quota. It produced exactly the same vector and output hash on the ID-rotation counterexample, so it was rejected and the source was restored byte-for-byte.

## D. Real database results

The source was verified anew: 32 schedules, 321 enrollments, 35 availability groups and 5,817 availability-day entries. No enrollment was already Priority. The earlier requested 34-schedule file was not available; the supplied local database is the explicitly identified source for this audit.

All 32 saved schedules were reconstructed using their actual settings, enrollment, minimums, availability and other saved assignments. Anonymous `legacy-real-000` is the first schedule in ascending stored-ID order; application `case-001` uses that same order. All 32 direct/application output hashes agree. All were replayable; no missing historical input was invented. Explicit no-Priority direct Legacy replay and the application preview route were checked separately. Each saved case has three measured deterministic repetitions. Application preview must write zero slots and leave the EF change tracker unchanged.

The named November graph was not saved in this source. The November case is explicitly a reconstruction from the actual availability group and the selected group's latest saved October roster/ordinary minimum targets, with the requested 12-person configuration, Priority minimum 170h, staffing four, adjacent six-hour templates, 200h cap, five-day work streak and full-day limits 2/5. Other November commitments remain in force. It is not presented as a historical regeneration.

Legacy additionally ran eight in-memory variations on four representative source schedules: zero/high minimums, higher staffing, tight cap, restricted availability, reduced roster, an added synthetic worker and strict full-day limits. Reversed input and ID rotation are separate variants. Including the reconstructed November no-Priority case gives 73 Legacy cases. The added synthetic participant has default availability solely as a controlled experiment.

Priority real-data cases separate both availability policies, one/two/several/all Priority workers, minimum variations, early/late single and coordinated anchors, reversed input, ID rotations, and external commitments. A separate 16-case real policy matrix crosses 6/8/10/12-hour durations, availability ON/OFF and WeekendsOff ON/OFF. No optimization percentage mixes different policies or targets.

The original database was never opened by SQLite. DB and WAL were byte-copied with fingerprints verified around the copy; recovery/checkpointing operated only on a private copy. Replay uses read-only, pooling-disabled, no-tracking/query-only access. Original DB/WAL/SHM byte hashes, sizes and modification times are compared before and after. Source personal data, IDs and raw availability are excluded from committed evidence.

## E. Independent optimality evidence

`LegacyScheduleReference` calls no production generator, candidate filter or scoring helper. It independently computes the nine-component objective and enumerates clipped template/window intervals plus remaining-cap prefix/suffix options. Forty-eight fixed tiny instances use two/three employees, two/three active dates, varied NONE/ANY/partial windows, unequal/impossible minima and hour/streak/full-day limits. A 200,000-state reference ceiling throws INCONCLUSIVE rather than claiming infeasibility. Every retained best witness and generated output is independently checked for hard safety.

The frozen Legacy failed 12 of the 48 quality comparisons. The final implementation matches all 48 best reference vectors. This proves `OPTIMAL_WITHIN_REFERENCE_SPACE` only; arbitrary interval endpoints and larger months are outside that proof. Existing Priority tiny references independently enumerate complete ordinary assignments together with exact early/late reservations at three ordinary targets. The existing exhaustive Priority anchor references, calendar-capacity, ownership and coordinated exchange regressions remain enabled.

Larger-case local searches are capped at 20,000 states. A limited search finding no improvement is INCONCLUSIVE as an optimality claim. A generated schedule with uncovered demand is PARTIAL; it is not automatically PROVEN_INFEASIBLE. Individual optimistic capacity bounds prove only their explicitly relaxed individual deficit lower bounds. The reconstruction's 120-minute Priority deficit equals its individual calendar-capacity lower bound, but that does not prove the whole schedule globally optimal.

## F. Before/after results — both engines

Legacy reconstruction, no Priority, identical adjacent-template demand of 86,400 employee-minutes:

| Metric | Before | After | Change |
| --- | --- | --- | --- |
| Covered employee-minutes | 84,930 | 85,290 | 360 |
| Uncovered employee-minutes | 1,470 | 1,110 | -360 |
| Priority minimum deficit minutes | 0 | 0 | 0 |
| Ordinary minimum deficit minutes | 14,130 | 13,110 | -1,020 |
| Normalized deficit squares (capacity diagnostic) | 0.5829 | 0.5655 | -0.0174 |
| Full objective vector | `[3,1470,235.5,6674.75,118.5,2087.75,121,4,0]` | `[2,1110,218.5,6211.75,101.5,1624.75,136,3,0]` | strictly better |
| Unfilled positions | 4 | 3 | -1 |
| Hard violations | 0 | 0 | 0 |
| Median plain runtime ms | 2,390.0733 | 2,627.3409 | 1.10x |
| Allocated MiB / generation | 1,199.275 | 1,203.0541 | 1.00x |

The Legacy normalized row is an independent diagnostic, `sum((minimum deficit / max(1,optimistic capacity))²)`; it is not an added objective component. The full vector retains unnormalized minimum and desired deficit squares.

Priority reconstruction, RespectAvailability ON, WeekendsOff ON, duration 8h:

| Metric | Before | After | Change |
| --- | --- | --- | --- |
| Covered employee-minutes | 81,810 | 82,140 | 330 |
| Uncovered employee-minutes | 4,590 | 4,260 | -330 |
| Priority minimum deficit minutes | 120 | 120 | 0 |
| Ordinary minimum deficit minutes | 17,190 | 17,160 | -30 |
| Normalized deficit squares | 0.5836 | 0.5823 | -0.0013 |
| Full objective vector | `[120,4590,17190,0.583580732169284,7.16558909331568,159,12]` | `[120,4260,17160,0.582315300070518,7.20786856146694,177,11]` | strictly better |
| Unfilled positions | 12 | 11 | -1 |
| Hard violations | 0 | 0 | 0 |
| Median plain runtime ms | 6,411.4607 | 9,211.7791 | 1.44x |
| Allocated MiB / generation | 2,470.6562 | 3,493.7062 | 1.41x |

Priority reconstruction, RespectAvailability OFF, WeekendsOff ON, duration 8h:

| Metric | Before | After | Change |
| --- | --- | --- | --- |
| Covered employee-minutes | 81,810 | 82,140 | 330 |
| Uncovered employee-minutes | 4,590 | 4,260 | -330 |
| Priority minimum deficit minutes | 120 | 120 | 0 |
| Ordinary minimum deficit minutes | 17,190 | 17,160 | -30 |
| Normalized deficit squares | 0.5836 | 0.5823 | -0.0013 |
| Full objective vector | `[120,4590,17190,0.583580732169284,7.16558909331568,159,12]` | `[120,4260,17160,0.582315300070518,7.20786856146694,177,11]` | strictly better |
| Unfilled positions | 12 | 11 | -1 |
| Hard violations | 0 | 0 | 0 |
| Median plain runtime ms | 6,629.3847 | 9,370.5716 | 1.41x |
| Allocated MiB / generation | 2,470.6337 | 3,493.7106 | 1.41x |

Priority ordinary utilization penalty rises from 7.165589093315676 to 7.2078685614669435 and its work-run penalty rises from 159 to 177. The 330-minute coverage gain has higher lexicographic priority. No weighted compensation or objective change is used.


## G. Discovered patterns

- All 32 unmodified real Legacy cases improve or tie the complete objective. Across the 73-case Legacy set, 67 valid baseline cases improve, two tie, one reversed-input result is worse, and three previously invalid baseline cases become valid. Invalid output is never credited as a coverage advantage.
- In the 132-case no-Priority matrix, nine baseline cases cross real template gaps; all final cases are hard-valid. Of the other 123, 22 improve and 101 tie the full vector. In the 44-case mixed matrix, one baseline no-Priority case is hard-invalid; three other cases improve and 40 tie.
- Four-worker/three-day and partial-window cases expose capacity coupling that pair ownership alone cannot resolve. The 48 tiny references retain these interactions as regressions.
- Canonical list determinism and employee-ID tie sensitivity are different. Sorting makes unchanged logical input stable; renaming IDs can still change valid greedy choices. Unequal hours alone do not establish unfairness.
- Coverage/target infeasibility and search limitation remain distinct. Restricting a real roster or imposing a tight cap can require less valid coverage than an invalid baseline appeared to provide.

The reconstructed workload also exposes legitimate distribution tradeoffs:

| Aggregate | Legacy before / after | Priority before / after (availability ON) |
| --- | --- | --- |
| Minimum assigned minutes per employee | 360 / 360 | 720 / 720 |
| Maximum assigned minutes per employee | 10,170 / 10,260 | 10,080 / 10,080 |
| Assigned-minute standard deviation | 3,194.48 / 3,325.68 | 2,895.30 / 2,853.68 |
| Two-position workdays across roster | 46 / 46 | 37 / 38 |
| Intervals shorter than six hours | 23 / 23 | 26 / 26 |
| Longest working run in roster | 5 / 5 | 5 / 5 |

The Legacy standard deviation rises while its higher-priority conflict, coverage and minimum components improve. This is not scored as a regression merely because hours become more unequal. The Priority utilization and working-run penalties also need not improve when coverage improves.

| Matching comparison set | Improved | Equal | Worse | Invalid baseline corrected |
| --- | --- | --- | --- | --- |
| legacy | 67 | 2 | 1 | 3 |
| legacy-matrix | 22 | 101 | 0 | 9 |
| matrix | 3 | 40 | 0 | 1 |
| real / availability True | 15 | 4 | 1 | 0 |
| real / availability False | 15 | 4 | 1 | 0 |
| policy-matrix / availability True | 4 | 3 | 1 | 0 |
| policy-matrix / availability False | 4 | 2 | 2 | 0 |

Rows compare identical inputs and policies. Repeated cases across sections are not unique source schedules. Mixed sets use the complete Legacy vector whenever Priority count is zero. Cross-version independent comparisons tolerate 1e-7 floating rounding; production Legacy retains EPS=1e-9 and Priority retains exact component comparison in canonical roster order. The candidate is not a monotonic quality upgrade for every counterfactual input.


## H. Confirmed bugs, regressions and changed files

- `BusinessLogicLayer/Generators/ScheduleGenerator.cs`: canonical roster, gap-safe intervals, materialization safety, remaining capacity fill, complete fixed-interval scoring and bounded small-calendar repair.
- `BusinessLogicLayer/Generators/PriorityScheduleGenerator.cs`: equivalent-lane pruning, canonical cloned guidance roster, cached bounded restart ranking/accumulation and final pair closure.
- `BusinessLogicLayer/Generators/PriorityGenerationAudit.cs`: opt-in aggregate search/ranking/evaluation counters; no public application contract change.
- `GF3.Tests/Infrastructure/LegacyScheduleReference.cs`: independent Legacy reference scorer/enumerator.
- `GF3.Tests/SchedulingComprehensiveOptimizationTests.cs`: 48 tiny references, canonical permutations, gap/materialization regressions and late-date Priority budget regression.
- `GF3.Tests/Infrastructure/ScheduleGenerationOracle.cs` and `tools/AlgorithmStressHarness/ReplayReportWriter.cs`: operating-union gap safety for all employees, including Legacy.
- `GF3.Tests/PriorityScheduleGeneratorTests.cs`: the quota-exhaustion fixture increases from 80 to 200 employees so it still actually reaches 20,000 after redundant lane trials are removed; the exact assertion remains.
- `GF3.Tests/PrioritySchedulingDeepAuditTests.cs`: restart non-regression compares final output with retained earlier feasible full vectors, as well as the pre-restart vector. A strict improvement specifically during the restart stage is not required when an earlier phase already improves the output. Guidance reuse and the two-restart limit remain asserted.
- `tools/AlgorithmStressHarness/PriorityDeepAudit.cs` and its project: independent Legacy scoring, separate before/after cases, complete objective evidence, in-memory source variants, policy matrix, deterministic repeats, performance and cancellation evidence. Reference code is compile-linked without a new dependency.
- This report and the accompanying anonymous results artifact retain aggregate evidence.

Red regressions were actually executed on frozen assemblies: 12 Legacy reference failures, enrollment permutation failure, separated-template bridge failure, 240-minute Priority late-date deficit, and synthetic materialization seed 0. Resolved tool failures included missing native SQLite runtime files in an initial isolated copy (fixed by copying the full runtime tree) and sandbox-blocked MSBuild IPC (verified builds rerun outside that restriction). Preliminary runs of superseded candidates and overnight timing samples are excluded from final benchmark comparisons.

No new task changes were made to UI, API contracts, schemas, migrations, dependencies or persistence services. Existing uncommitted changes from the previous work were preserved. No commit, reset, checkout, push or source-data mutation was performed.

## I. Remaining suboptimal witnesses

The canonical Legacy result for anonymous `legacy-real-016` and its reversed-list variant is `[0,0,60,722,52,554,7109,0,0]`. The frozen algorithm happened to produce `[0,0,60,720,52,560,7124,0,0]` on the reversed list. Both are independently hard-valid. The old witness wins at squared minimum deficit (720 < 722), despite worse desired-hour square and rest. This is `VALID_BUT_SUBOPTIMAL`; input-order stability does not erase the stronger witness. The unmodified source-order baseline was `[0,0,60,722,52,554,7121,0,0]`, which the final result improves. Larger coordinated ownership changes remain an unresolved search limitation.


Availability True: 3 realized offline configurations beat normal production. Best `counterfactual-respect-True-day-24` gives `[120,4230,17130,0.580980397023427,7.21306737111728,177,11]`, compared with normal `[120,4260,17160,0.582315300070518,7.20786856146694,177,11]`. It covers another 30 minutes and reduces ordinary deficit by 30 minutes. The extra fixed starting anchor plus the bounded restarts is a larger search than the normal two-restart construction. It is independently hard-valid.

Availability False: 3 realized offline configurations beat normal production. Best `counterfactual-respect-False-day-24` gives `[120,4230,17130,0.580980397023427,7.21306737111728,177,11]`, compared with normal `[120,4260,17160,0.582315300070518,7.20786856146694,177,11]`. It covers another 30 minutes and reduces ordinary deficit by 30 minutes. The extra fixed starting anchor plus the bounded restarts is a larger search than the normal two-restart construction. It is independently hard-valid.

Other retained valid baseline witnesses:

| Case | Final vector | Better frozen vector |
| --- | --- | --- |
| real-id-permutation-1-respect-True | `[120,4740,17640,0.595894370553744,7.14839775691811,159,12]` | `[120,4740,17160,0.58348292087692,7.15631273665315,168,12]` |
| real-id-permutation-1-respect-False | `[120,4740,17640,0.595894370553744,7.14839775691811,159,12]` | `[120,4740,17160,0.58348292087692,7.15631273665315,168,12]` |
| real-hours-6-respect-True-weekends-True | `[2640,6390,16890,0.770349232490552,7.19604862024215,177,14]` | `[2640,6330,17100,0.781138121379441,7.15246324051953,177,14]` |
| real-hours-6-respect-False-weekends-True | `[2640,6390,16890,0.770349232490552,7.19604862024215,177,14]` | `[2640,6330,17100,0.781138121379441,7.15246324051953,177,14]` |
| real-hours-6-respect-False-weekends-False | `[2640,6390,16890,0.770349232490552,7.19604862024215,177,14]` | `[2640,6330,17100,0.781138121379441,7.15246324051953,177,14]` |

The two ID-rotation cases lose 480 ordinary minimum minutes with unchanged coverage. The three six-hour policy cases lose 60 coverage minutes while improving the lower-priority ordinary deficit by 210 minutes; the frozen vector therefore remains better. These are real search regressions, not claimed fairness improvements under the established objective. They are reported rather than hidden by the normal eight-hour improvement. All accepted moves within the new search still require strict improvement of the unchanged full vector.

All 72 forced configurations were realized. The independent bounded local scan found 0 additional improvement witnesses in the final replay sets; capped scans do not establish optimality. Full aggregate vectors, policies, hashes and independent tiny-reference before/after evidence are retained in [anonymous results](schedule-generation-comprehensive-results.json).


## J. Verification and performance

Environment: Windows x64, .NET SDK 10.0.401 / runtime 10.0.12, Node 22.14.0. Branch `DEV2`, HEAD `603735907c2ccf2c5931c98d956166e1f080a421`; both implementations include uncommitted work. Frozen generator SHA-256 `E69B33B7812C2EB736B34FE1F6113814CA15C0D378716CE27516B15A0185448F`; final generator SHA-256 `DD7049B108792D4E148C2B61EC71D967409025690BFDF48D57B2A6A5C8AE84A1`. The committed checkout alone is not the baseline. A full dependency/native runtime tree and matching driver preserve executable E69 evidence.

| Executed check | Result |
| --- | --- |
| `dotnet restore GF3.sln -m:1 -nr:false` | Passed; dependencies up to date |
| `dotnet build GF3.sln --no-restore -m:1 -nr:false` | Passed; 0 warnings/errors; final rebuild 7.54s |
| Harness build with the same serialized MSBuild options | Passed; 0 warnings/errors |
| Ordinary `Category!=GenerationStress` | 1,533 passed, 0 failed/skipped; 17m25s |
| `Category=GenerationStress` | 400 passed, 0 failed/skipped; 18m19s; original seeds 1000–1199 for each engine |
| Final focused reference/materialization/restart checks | 56 passed, 0 failed/skipped; 1m32s |
| Direct Legacy saved replay, frozen baseline | 32/32; three deterministic repeats, 0 failures/skips |
| Application preview saved replay, before and after | 32/32 each; three deterministic repeats; zero persistence writes |
| Final direct Legacy / application saved output hashes | 32/32 identical |
| Explicit Legacy real and controlled cases | 73 final cases, three measured repeats each; 0 hard violations |
| No-Priority / mixed pairwise cases | 132 / 44 final cases, three measured repeats each; 0 hard violations |
| Priority real/controlled/offline cases | 112 final cases: 40 normal x3 measured repeats, 72 forced runs; 0 hard violations |
| Real duration/availability/weekend matrix | 16 final cases x3 measured repeats; 0 hard violations |
| Known-witness production assertions | Both availability policies reproduce the retained 4,260-minute vector |
| Independent Legacy tiny optima | 48/48 match; maximum 1,964 reference states, no reference timeout |
| `manager-priority.spec.ts`, all four Playwright projects | 8 passed; 1.3min |

The requested test category commands used `--no-build --no-restore -p:OutputPath=bin/ComprehensiveFinal2/net10.0/` after a successful isolated build, with test-collection parallelism disabled. Original stress seeds remain enabled. Superseded/aborted preliminary builds and overnight timing samples are excluded from final results. The frozen control exits nonzero for its observed hard-invalid baseline cases (3 Legacy real variants, 9 no-Priority matrix cases, 1 mixed matrix case); final candidates exit zero. These negative controls are not misreported as passing generators.

Playwright uses mocked API responses. It verifies frontend settings/preview/save/reload behavior; actual backend integration tests cover generation, persistence, protected manual offers, transaction rollback, cancellation/retry and concurrent operations. Browser checks do not prove the scheduling optimum or a deployed backend. No physical device or production deployment was tested. Frontend/API source was not changed in this task, so frontend lint/unit/build were not rerun or claimed as fresh results.


Timing protocol: frozen E69 and final DD7049 use identical inputs and business rules, one discarded warmup and three measured plain generations per benchmark case. Diagnostic runs and offline local searches are separate. Allocation counts are whole-generation GC allocation totals, not live heap size. Process peak working set is a cumulative process high-water mark and includes audit overhead; it is not attributed solely to one case. Audit runs performed concurrently with tests are useful quality/determinism evidence, not isolated throughput benchmarks.

| Isolated case | Before median ms | After median ms | After max ms | Time ratio | Before alloc MiB | After alloc MiB |
| --- | --- | --- | --- | --- | --- | --- |
| legacy-reconstruction | 2,390.0733 | 2,627.3409 | 2,649.6182 | 1.10x | 1,199.275 | 1,203.0541 |
| reconstruction-respect-True | 6,411.4607 | 9,211.7791 | 9,231.3411 | 1.44x | 2,470.6562 | 3,493.7062 |
| real-priority-count-6-respect-True | 3,051.421 | 3,170.9542 | 3,259.9343 | 1.04x | 1,860.0824 | 1,880.8606 |
| real-priority-count-12-respect-True | 133.2077 | 141.4356 | 153.3924 | 1.06x | 125.9919 | 125.9958 |
| real-target-0-respect-True | 6,008.3716 | 8,461.3045 | 8,650.5213 | 1.41x | 2,491.8659 | 3,210.2306 |
| reconstruction-respect-False | 6,629.3847 | 9,370.5716 | 9,501.1868 | 1.41x | 2,470.6337 | 3,493.7106 |
| real-priority-count-6-respect-False | 2,409.4641 | 2,461.3032 | 2,472.0692 | 1.02x | 1,805.7164 | 1,818.9724 |
| real-priority-count-12-respect-False | 272.0126 | 273.583 | 277.64 | 1.01x | 256.9133 | 256.9196 |
| real-target-0-respect-False | 6,152.0683 | 8,289.9923 | 8,449.1374 | 1.35x | 2,491.8688 | 3,210.2315 |

Each revision completed 33 Legacy cases (all 32 saved graphs plus reconstruction) and eight Priority cases, with one warmup and three measured plain repetitions per case. Candidate diagnostic parity runs are outside these timings.

| Process run | Cases | Before cumulative peak MiB | After cumulative peak MiB |
| --- | --- | --- | --- |
| legacy-benchmark | 33 | 111.1562 | 111.2109 |
| benchmark | 8 | 104.0859 | 107.6523 |

Cases exceeding 2x their frozen same-input median:

| Case | Before ms | After ms | Ratio |
| --- | --- | --- | --- |
| legacy-real-000 | 11.3798 | 23.0247 | 2.02x |
| legacy-real-001 | 12.8425 | 74.8777 | 5.83x |
| legacy-real-003 | 25.6285 | 51.9782 | 2.03x |
| legacy-real-004 | 151.4285 | 523.9794 | 3.46x |
| legacy-real-005 | 17.7609 | 38.2058 | 2.15x |
| legacy-real-006 | 26.9301 | 99.6862 | 3.70x |
| legacy-real-008 | 83.2445 | 259.3843 | 3.12x |
| legacy-real-009 | 50.3861 | 253.3113 | 5.03x |
| legacy-real-010 | 20.9234 | 45.7278 | 2.19x |
| legacy-real-011 | 13.6937 | 52.039 | 3.80x |
| legacy-real-015 | 57.0889 | 115.6063 | 2.03x |
| legacy-real-018 | 15.9247 | 46.7767 | 2.94x |
| legacy-real-020 | 89.9421 | 401.4198 | 4.46x |
| legacy-real-023 | 22.3761 | 52.4149 | 2.34x |
| legacy-real-024 | 8.6825 | 22.038 | 2.54x |
| legacy-real-026 | 15.3977 | 90.0862 | 5.85x |
| legacy-real-027 | 51.8004 | 149.305 | 2.88x |

The eight-hour ON-policy reconstruction gains 330 covered employee-minutes for 2.800s additional median time (117.8 employee-minutes per added second), with 1.41x gross allocation. Its separate diagnostic ranking took 2256.5ms for 21 cached projections and 8 one-pass refinements; it built Legacy guidance 1 time and performed 2 complete restarts. Diagnostic phase timing is not subtracted from a different plain-run timing as a causal decomposition.

All 17 over-2x cases are Legacy saved cases. Investigation identified the added desired/rest ownership polishing after the original minimum repair; previously inactive secondary optimization now does useful work. Their final medians range from 22.04 to 523.98ms, and the largest relative ratio is 5.85x (15.40 to 90.09ms). The largest absolute regression in this subset is 151.43 to 523.98ms. These are material relative regressions and are retained transparently alongside the complete quality vectors, rather than called a speed improvement. The reconstructed Legacy month is about 2.63s versus 2.39s.

Older Legacy stages do not expose comprehensive candidate counters, so those counts are unmeasured, not zero. The new tiny component has a strict 20,000-state bound. Priority root/ranking/restart evaluation totals are separated above; adding ranking does increase total search work even though existing per-construction quotas remain unchanged. No new external solver/dependency or unbounded normal-request recursion was added.


Cancellation probes requested cancellation after 1ms and 25ms on both real reconstructed engines. Cancellation was observed with unchanged input in all four cases; measured elapsed times were Legacy 25.64/36.56ms and Priority 10.28/45.24ms. These are observations, not hard latency guarantees. Existing mid-construction/restart cancellation, retry, concurrent calls, preview/save/reload/delete, protected manual positions and transaction rollback tests remain enabled.

## K. Final technical assessment

Both engines now have stronger independently checked feasibility and demonstrated full-vector improvements. The normal Priority path reaches the known 4,260-minute-gap witness without increasing every existing search quota; Legacy corrects actual hard-invalid materializations and passes small independent reference optima. Neither engine is a global optimizer. Bounded monthly searches, ID tie sensitivity and the explicitly retained larger Legacy witness remain practical limits.

All final builds, 1,933 backend tests, eight browser checks and accepted replay outputs above pass. Original DB/WAL/SHM hashes, sizes and modification times are identical before and after; a fresh private-copy integrity check returned `ok` with the same four table counts.

Hard-safety and workflow evidence supports practical use on the tested workloads, with the measured latency/allocation costs. This is not a universal quality upgrade: the six retained valid baseline counterexamples (one Legacy reversed-list case, two Priority ID rotations and three six-hour policy cases) and the stronger 4,230-minute offline witness remain unresolved optimization limitations. Larger/high-concurrency production workloads were not capacity-tested. Deployability should be judged with those measured costs and counterexamples visible; global optimality and per-input monotonic quality are not claimed.
