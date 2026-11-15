# Priority scheduling deep audit

Audit date: 2026-10-28. Checkout: `DEV2`, `603735907c2ccf2c5931c98d956166e1f080a421`, with the pre-existing Priority Availability implementation retained. This report concerns the subsequent generator audit; it does not attribute those earlier settings/API/frontend changes to this audit. No commit, publication, migration of the original database, or production request was performed.

## A. Executive summary

The morning-heavy result has two distinct causes. Equivalent solutions deterministically prefer the earlier candidate, and the objective contains no early/late balance preference. That is expected behavior. However, the earlier generator also overlooked demonstrably better complete-team schedules. An early reservation changes the clipping of ordinary-worker legacy guidance and can trap monthly allocation; rebuilding only one affected day cannot always escape that trap.

Four demonstrated weaknesses were corrected: reservation spacing that needlessly loses feasible Priority workdays; single-day search that misses a beneficial atomic two-day change; dependence on the initial anchor despite a better complete monthly construction; and Priority ownership locks that prevent a better normalized-deficit distribution between Priority workers. The fixes preserve exact durations, availability policy, external commitments, staffing, hour/streak/full-day limits, and the existing lexicographic objective. They add no arbitrary morning/evening balancing rule or dependency. The legacy generator was not changed.

In the real-data reconstruction, both availability policies change from **21 early / 0 late** to **19 early / 2 late**, with **360 more covered employee-minutes**, **360 fewer regular-worker deficit minutes**, and no hard violations. The remaining Priority deficit of **120 minutes** is provably unavoidable under the configured weekday-only, exact-eight-hour policy. This does **not** prove the remaining ordinary allocation optimal: the deeper offline counterfactual search can still find better monthly witnesses than the bounded production search. Those results are classified explicitly below.

## B. Exact reproduction and source protection

The screenshot images themselves were not attached to this request. The numerical configuration in the request was used as the screenshot specification. The supplied current database differs materially from that specification:

| Item | Supplied snapshot | Reconstruction |
| --- | --- | --- |
| Saved F35 November 2026 schedule | Absent, checked through schedule and shop/group relationships | Unsaved in-memory scenario |
| F35 November availability group | Present, 12 members | Actual selected group |
| Priority enrollment flags | Zero across 321 enrollments | One employee marked Priority in memory |
| Settings | 8 hours, weekends off, respect availability off | Both respect policies tested separately |
| Saved schedules | 32; seven each July–October and four November | All 32 replayed separately |
| Availability | 35 groups, 5,817 day entries | Actual November group; no invented source rows |
| Ordinary minimum targets | No saved target-month roster | Latest available F35 October roster |
| External work | Six relevant saved assignments for the selected Priority employee | All relevant November commitments protected |

The reconstructed dimensions are November 2026; 12 employees; staffing four; 09:00–15:00 and 15:00–21:00; monthly cap 200 hours; maximum work streak five; maximum full-day streak two; maximum full days five; Priority duration eight hours and target 170 hours. The imported minimum-target histogram, including Priority, is `0×1, 12×1, 90×1, 145×3, 150×1, 170×1, 190×4` hours. These are month-level aggregates, not personal records.

This is a **real-availability reconstruction**, not an exact replay of an existing F35 November schedule or the unavailable screenshot. A separate synthetic 12-employee scenario reproduces every supplied numerical dimension, with explicitly synthetic ordinary targets and all-available input. It tests all nine requested target values under all four policy/weekend combinations. Its output must not be presented as source data.

Only byte reads were used on the original database and its WAL/SHM sidecars. A before/after-stable file copy preserved the WAL; checkpointing and integrity queries occurred only on a disposable private copy. Harness connections use read-only mode, no pooling, no-tracking reads, and `PRAGMA query_only=1`. No schema fallback or repair was performed. Reports contain aggregate metrics and anonymous ordinals; raw availability, names, source identifiers, and private source paths are excluded from this document.

### The 170 versus 168 hours question

November 2026 has 21 weekdays. With one exact eight-hour Priority interval per weekday, the upper bound is `21 × 8 = 168` hours. All 21 weekdays have feasible early and late anchors in this reconstruction. The six external commitments block weekend dates and do not explain the weekday orientation. A valid 168-hour construction meets the bound, so the missing two hours are **INFEASIBLE — PROVEN**, under these assumptions. The generator neither extends a shift to nine hours nor adds prohibited weekend work.

The existing policy is preserved: respect availability **off** synthesizes weekday availability even if `WeekendsOff=false`. Respect availability **on** with `WeekendsOff=false` can use weekends. The audit does not redefine this contract. Single-Priority exhaustive settings/target tests reach 200 hours when weekends are permitted and preserve 168 otherwise, including targets 0, 1, 7, 40, 80, 120, 160, 168, 169, 170, 180, and 200. In a mixed roster, a zero minimum is not an obligation to assign Priority work after ordinary workers already cover the demand.

## C. Root cause analysis

### Candidate generation, reservation, and search

`PriorityScheduleRules.BuildPriorityIntervals` creates exact-duration earliest/latest intervals inside each continuous operating envelope, preserving the existing positive-overlap availability semantics. Genuine operating gaps are not bridged. An eight-hour day operating 09:00–21:00 offers 09:00–17:00 and 13:00–21:00. External work is an exact exclusion, not an availability preference.

Candidates are generated in canonical order. Reservation compares relative need, scarcity, interval length, future day capacity, current day load, work streak, employee ID, day, and start time. Before this audit the future-day-capacity term was absent. A complete tie therefore chooses the earlier start, and lower employee IDs can decide equal competition. Reversing caller lists is neutralized by canonical ordering; changing the IDs can change the heuristic path.

Priority reservations are locked against ordinary ownership exchanges. The existing dedicated reorientation stage can move them in either direction, remove ordinary assignments on the affected date, refill that date, validate the entire trial, and accept only a strict full-objective improvement. It is not equivalent to reconstructing the complete month: ordinary workers on other dates retain hours that can prevent a locally promising refill. Legacy guidance was also clipped against the initial reservations, so a different initial anchor can lead to a better whole-month construction even when day-local moves find nothing.

The original single-anchor limit remains **64 completed trials / 20,000 assignment validations**, with two passes. On the reconstruction it reaches the trial limit after only 1,720 validations. Repeated shift/lane placements consume trials; having generated 21 feasible late candidates does not mean every date and complete-team alternative was explored. The additional atomic two-date stage has its own limit of **16 completed trials / 10,000 validations**. Up to **two** full construction restarts reuse the one already-computed legacy guidance result; they cannot recursively restart. Each construction retains its own bounded original phases, so these are not a claim that the whole generation uses only 20,000 validations. Cancellation is observed through every phase, and rejected trials restore the accepted state.

An additional Priority-to-Priority ownership pass preserves each interval and position. It uses only the remaining existing 50,000-validation improvement quota, with at most six passes, and skips single-Priority inputs. A receiver must have the exact valid candidate on that date; donor diagnostic anchors cannot be removed. Every transfer requires a strict full-vector improvement, assignment validation, and whole-state validation. This addresses Priority hour fairness without changing time orientation, coverage, or the objective. It runs after the accepted original and coordinated searches, so it cannot undo their quality.

Restarts are skipped when there are no ordinary workers, no legacy guidance, or all three primary demand/deficit components are zero. Exact calendar capacity has a fast path when all available-day runs already satisfy the streak limit. These avoid paying search costs when the added search cannot address the demonstrated weakness.

In the reconstructed base call, the two accepted restart vectors are `[120,4950,17550,0.5932816863090189,7.1216182887668396,168,13]` and `[120,4590,17190,0.5835807321692836,7.165589093315676,159,12]`. The first improves normalized deficit while preserving the three primary components; the second improves coverage and ordinary deficit. Lower-priority utilization/streak scores are allowed to rise only after a higher-priority component has strictly improved.

### What the objective actually means

The seven components, minimized in order, remain:

1. Priority minimum deficit minutes.
2. Uncovered employee-minutes.
3. Ordinary minimum deficit minutes.
4. Sum of squared normalized employee deficits.
5. Sum of squared ordinary-worker utilization against optimistic individual capacity.
6. Sum of squared longest working-day streaks.
7. Unfilled position count.

Normalization uses the existing optimistic capacity estimate and target; it is not a proof of attainable individual hours. The objective rewards coverage before ordinary minimum attainment and hour-distribution fairness. It has **no component describing an unfavorable evening shift, employee preference, rotation entitlement, or equal early/late distribution**. Therefore it cannot establish fairness of shift orientation. No such business requirement was supplied, so adding a 50/50 term would invent a policy. Equivalent early/late solutions retain the deterministic earlier tie.

The reported ordinary utilization penalty rises in the improved real result, while coverage, ordinary deficit, and normalized deficit improve. This is a transparent consequence of the established priority order, not evidence that every fairness metric improved. A higher-priority improvement is never rejected or reversed for cosmetic orientation balance.

## D. Before/after comparison

The pinned control is the actual generator present at the start of this audit, including the earlier Priority Availability work, rather than the older Git HEAD implementation. Its assembly SHA-256 is `5DE04294D1C9E09736FEBF7A8E4570B0AEF6811C74C4E917F5D292013630A159`. The final candidate hash and timing methodology are recorded with the final measurements below. Both receive identical inputs and one warmup followed by three measured normal generations; optional diagnostics run separately and must reproduce the exact normal output. Allocations are cumulative allocated bytes per generation, not peak live memory.

For **each** respect-availability policy, with weekends off:

| Metric | Before | After | Change |
| --- | ---: | ---: | ---: |
| Priority early intervals | 21 | 19 | −2 |
| Priority late intervals | 0 | 2 | +2 |
| Covered employee-minutes | 81,450 | 81,810 | +360 |
| Uncovered employee-minutes | 4,950 | 4,590 | −360 |
| Priority minimum deficit | 120 | 120 | 0 |
| Ordinary minimum deficit | 17,550 | 17,190 | −360 |
| Squared normalized deficit | 0.5947718679 | 0.5835807322 | −0.0111911357 |
| Ordinary utilization penalty | 7.1186890933 | 7.1655890933 | +0.0469 |
| Longest-streak penalty | 159 | 159 | 0 |
| Unfilled positions | 13 | 12 | −1 |
| Hard violations | 0 | 0 | 0 |
| Median generation time, respect ON | 3,431.8 ms | 6,474.5 ms | +3,042.7 ms (1.89×) |
| Median generation time, respect OFF | 3,399.1 ms | 6,432.7 ms | +3,033.6 ms (1.89×) |

Full vectors are `[120,4950,17550,0.5947718679033557,7.118689093315675,159,13]` and `[120,4590,17190,0.5835807321692836,7.165589093315676,159,12]`. A valid staffing-bounded interval contributes its duration once to coverage; vacancy count is a separate position metric.

The independent optimistic bounds for this input are: demand 86,400 minutes, enrolled minima 97,020, individual capacity sum 101,340, coverage-gap lower bound 2,520, and total minimum-deficit lower bound 10,620. Final coverage gap exceeds its bound by 2,070 and combined deficit exceeds its bound by 6,690. These lower bounds relax coupled constraints and do not prove that those differences can all be removed. Ordinary minima total 86,820 minutes while only 76,320 minutes of demand remain after 168 Priority hours: all ordinary minima cannot be achieved simultaneously.

## E. Counterfactual evidence and independent references

The offline audit constrains an anchor only in an internal diagnostic scenario and regenerates ordinary work through the complete pipeline. It verifies that the requested anchor actually appears before calling the trial a feasible witness. A request that was not realized is not an improvement or an infeasibility proof by itself.

| Controlled one-active-day case | Forced early coverage | Forced late coverage | Finding |
| --- | ---: | ---: | --- |
| Ordinary worker only 09:00–13:00 | 480 | 720 | Late Priority is better; ordinary deficit 240→0 |
| Ordinary worker only 17:00–21:00 | 720 | 480 | Early Priority is better; ordinary deficit 0→240 |
| Ordinary worker available throughout | 720 | 720 | Full seven-component vectors equal |
| External Priority commitment 17:00–21:00 | 720 | Not realized | Late exact-eight-hour anchor conflicts; no invalid assignment accepted |

Numbers are covered employee-minutes. The month includes inactive NONE dates, so absolute monthly gap includes intentionally unavailable demand. All valid trials have zero hard violations. The existing generator already solved the two basic beneficial orientations; these controls establish that the fix has not blindly preferred evening.

The independent tiny reference enumerates complete ordinary assignments and both Priority orientations across two active days, three employees, and four positions. It does not call production candidate selection, reservation, refill, or optimization. Independent invariant and canonical-vector checks compare the production output against the minimum enumerated vector for ordinary targets 0, 4, and 8 hours. The state cap is 200,000; exceeding it fails as **INCONCLUSIVE — SEARCH BOUND EXHAUSTED**. Completed equal-vector results are **OPTIMAL WITHIN REFERENCE SPACE**, not global month-wide proofs.

There is also an independent exact-rational hours-count reference for the all-Priority, respect-OFF real variant. Every eligible eight-hour interval in a twelve-hour envelope overlaps the common 13:00–17:00 core. Staffing four therefore permits at most four Priority intervals per weekday: 84 intervals / 40,320 minutes in the month. All relevant external work is on excluded weekend dates. A dynamic program evaluates 15,972 count transitions, retaining at most 85 total-count states per employee layer, to minimize raw deficit and then normalized deficit over 0–21 shifts per worker and 84 total shifts. It calls no production code and uses exact rational arithmetic. Its minimum normalized deficit is `18536344627/4768470000 = 3.8872729884008916`, with raw deficit 56,700 minutes. Coverage gap 46,080 minutes is unavoidable in this all-Priority configuration and is attained. Count assignments can be placed on the 21 common weekdays with four distinct workers per date; weekends break all work streaks at five. This reference addresses the hours/deficit prefix only; it does not optimize date-dependent rest scores or establish an optimum for the complete seven-component month. The final candidate comparison is recorded below.

The reproducible count recurrence is `D_i(k) = lexmin_d [D_(i−1)(k−d) + (m_i(d), (m_i(d)/b_i)^2)]`, where `m_i(d)=max(0,T_i−480d)`, `b_i=max(1,min(T_i,10080))`, `0≤d≤min(21,k)`, `0≤k≤84`, `D_0(0)=(0,0)`, and other initial states are unreachable. Targets `T_i` are the anonymous table's hour targets converted to minutes. One optimal count vector in that order is `(7,9,9,10,10,1,8,7,7,8,8,0)`. Each count is at most 21 and their sum is 84; the complete worker/weekday eligibility graph with four daily positions admits those counts. The retained tiny C# enumeration tests separately provide executable independent full-vector reference coverage.

The real audit flips every assigned Priority date separately and all 15 pairs among the first six early dates, under both policies. These diagnostic searches have a larger combined exploration space than one production call. They can retain **VALID BUT SUBOPTIMAL** witnesses against the final production result; the bounded two-restart correction reduces the demonstrated defect but does not eliminate every monthly local optimum. Counts and best witnesses are recorded in the final measurements. Trials with no improvement are **PARTIAL — FEASIBILITY NOT PROVEN**, not optimality certificates.

## F. Discovered patterns and fairness

The 44-row deterministic pairwise matrix covers every declared factor pair across employee counts 1/2/4/8/12/20, staffing 1–4, Priority none/one/two/half/all, durations 6/8/10/12, both policies/weekend settings, six availability patterns, four target levels, 28/29/30/31-day months, four shift templates including overlap and a genuine gap, and four external-commitment patterns. Rows use seed `350112026`; individual mixed patterns use `3500 + row index`. Pairwise coverage is asserted independently of generation success.

| Pattern | Evidence / classification |
| --- | --- |
| Initial early tie preference | Equal-vector control retains early; expected deterministic tie, not a hard violation |
| Morning-only regular availability favors late; evening-only favors early | Two controlled reverse cases, +240 coverage for the appropriate orientation; demonstrated team-level effect |
| Respect OFF necessarily increases early bias | Not supported by the reconstructed base case: both policies have identical final metrics and output; source availability was already compatible on all weekdays |
| Higher Priority counts create competition | Real-roster counts 0/1/2/3/4/6/12 tested per policy; counts affect hours, feasibility and orientation together, so correlation does not establish unfairness |
| Candidate budget can hide useful late days | Reconstruction reaches 64 trials with feasible unexhausted calendar alternatives; demonstrated optimization weakness |
| Single-day changes can miss coupled improvements | Three-worker/four-active-day regression has a strict two-day witness; confirmed and corrected |
| Caller input order changes the Priority result | Reversed input tests preserve exact output on all 44 matrix cases; real reversed inputs also checked |
| Changing deterministic employee IDs changes allocation | Real ID rotations remap roster, availability and external work together; genuine heuristic sensitivity, not caller-order nondeterminism |
| Impossible Priority targets hide different normalized deficits | Two workers / three active days / staffing one, 30h and 40h targets with 24h capacity each; same total deficit but a strict normalized-deficit witness; confirmed and corrected by valid Priority ownership transfer |
| All morning results prove an error | Disproved by equivalent and early-better controls; morning prevalence alone is insufficient evidence |
| More late shifts imply better fairness | Disproved by lexicographic metrics: coverage can improve while the lower-priority utilization penalty rises |

Floating-point differences around `1e-16` in normalized deficit after ID permutations are reported as rounding noise, not meaningful quality improvements. Production comparison itself was not weakened or changed. Identical constraints can receive different deterministic time orientations because no orientation entitlement exists in the objective. The audit cannot label a particular residual shift “unfavorable” without a preference or policy definition.

Employee-level anonymized results below distinguish target deficits from optimistic individual capacity bounds. Capacity fractions are bounds-based diagnostics, not exact attainable percentages. Full-day counts mean multiple assigned cells on a date, following the existing domain semantics; partial intervals are shorter than six hours. Explicit full-day availability retains the existing domain override and is reviewed separately, rather than silently imposing a new labor-rule interpretation.

Both policies give this same reconstructed base result. P and R1–R11 are anonymous report labels, not source identifiers. Hours and bounds are monthly; “E/L” counts intervals and can count both cells of an ordinary full day.

| Worker | Target h | Optimistic bound h | Before h | After h | Deficit / bound | E/L | Full / partial | Longest run / days off |
| --- | ---: | ---: | ---: | ---: | ---: | --- | --- | --- |
| P | 170 | 168 | 168 | 168 | 1.19% | 19/2 | 0/0 | 5/9 |
| R1 | 190 | 200 | 156 | 158 | 16.00% | 13/14 | 5/2 | 4/8 |
| R2 | 190 | 200 | 156 | 156 | 17.00% | 14/12 | 5/0 | 4/9 |
| R3 | 190 | 200 | 154 | 158 | 16.00% | 15/12 | 6/2 | 5/9 |
| R4 | 190 | 198 | 144 | 144 | 23.23% | 13/11 | 5/0 | 5/11 |
| R5 | 12 | 18 | 12 | 12 | 0% | 2/0 | 0/0 | 1/28 |
| R6 | 145 | 108 | 108 | 108 | 34.26% | 2/16 | 1/0 | 3/13 |
| R7 | 90 | 91.5 | 79.5 | 79.5 | 11.48% | 5/10 | 3/6 | 3/18 |
| R8 | 150 | 188.5 | 135 | 135 | 7.96% | 14/11 | 5/9 | 4/10 |
| R9 | 145 | 102 | 102 | 102 | 42.16% | 6/12 | 3/3 | 3/15 |
| R10 | 145 | 108 | 108 | 108 | 34.26% | 10/8 | 4/0 | 2/16 |
| R11 | 0 | 107 | 35 | 35 | 0% | 2/5 | 0/4 | 2/23 |

R1 gains two hours and R3 four; no worker loses hours in this base comparison. The ordinary squared-utilization penalty increases because those two workers cover more demand, not because another worker lost time. R6, R9 and R10 already meet their optimistic capacity bounds: their 37/43/37-hour target shortages are individually proven under these bounds. R3's full-day count changes **5→6**: the sixth day triggers the existing explicit-availability override review, accepted by the independent domain checks. This is an observable workload change under an unchanged domain rule, not a claim that the configured five-day limit is absolute in every availability mode. R11's zero target is a minimum, not a maximum or a ban on assigning useful work. Differences in late/partial counts do not by themselves establish unequal treatment, because the individual availability windows differ.

The artificial **all-Priority / respect-OFF** variant has a different tradeoff. Ownership correction redistributes a fixed 672 hours: the 170-hour-target worker changes 72→56 hours, the 150-hour-target worker 64→56, the 90-hour-target worker 40→56, and one 190-hour-target worker 72→80. The four equivalent 190-hour-target workers end at 72/72/80/80 hours, a one-exact-shift spread. Aggregate Priority deficit is unchanged, normalized deficit improves to the independent hours-reference optimum, and the lower-priority streak penalty rises **11→23**. This is not a Pareto improvement for every person. The existing score minimizes aggregate Priority shortage followed by normalized distribution; it does not protect one named Priority worker ahead of the other Priority workers or require every individual's deficit to decrease on each accepted move. Those policy consequences are exposed rather than labeled universally fair.

## G. Confirmed defects and fixes

| Defect | Minimal evidence before fix | Root cause | Correction / retained regression |
| --- | --- | --- | --- |
| Avoidable Priority day loss | Eight available days, streak two, exact eight hours, target/cap 48: old output 40 hours; witness days 1,2 / 4,5 / 7,8 gives 48 | Minimizing immediate streak spreads reservations across dates that destroy future capacity | Exact calendar DP scores remaining attainable days containing reservations; cached per employee/day and invalidated on reservation; `PriorityReservationPreservesConstructivelyAttainableWorkdays` regression |
| Coupled anchors missed | Four active dates, three workers, staffing one, 16-hour caps: old vector `[0,19080,360,0.0703125,1.3203125,17,52]`; two-anchor witness `[0,18960,240,0.03125,1.53125,17,52]` | Each isolated change fails to improve the monthly vector; ordinary hour capacity is coupled across dates | Separate bounded atomic two-date refill after the complete original single-date stage; `CoordinatedAnchorsReleaseMonthlyCapacityThatSingleDayMovesCannotImprove` |
| Initial construction trap | Real reconstruction old gap 4,950; a changed initial anchor and full reconstruction yield 4,590 despite no accepted local reorientation | Initial reservations alter month-wide clipping and worker allocation; affected-day refill cannot recover that history | At most two complete constructions with cached guidance and different valid initial anchors; strict whole-vector comparison and full validation; three deterministic mixed-month regressions, seeds 11/29/47 |
| Priority ownership fairness trap | Two Priority workers / three active days, targets 30/40h, cap 24h each: old work 16/8h; valid work 8/16h preserves deficit 2,760 and gap 20,160 minutes but improves normalized deficit 2.1180556→1.8402778 | Reservation urgency clips impossible targets to equal capacity, while locked Priority ownership excludes later correction | Bounded validated ownership transfers using the remaining original improvement quota; `ImpossiblePriorityTargetsPermitAValidOwnershipTransferWithLowerNormalizedDeficit` observed red, then green |

The calendar, paired, and Priority ownership regressions were observed failing before their corrections, with actual-versus-witness values retained in the local TRX evidence. The monthly regressions require a **strict** improvement over the accepted result before restarts, exactly one legacy-guidance build, and at most two restarts. The three seed cases reduce coverage gaps by 60, 120, and 240 minutes respectively. An attempted reduction of the original single-stage trial quota caused a matrix regression and was rejected; the final implementation preserves the original 64-trial search before adding new stages. The ownership witness increases the lower-priority streak penalty 2→5 while strictly improving normalized deficit; the unchanged lexicographic ordering requires accepting that improvement.

Audit diagnostics are internal and opt-in. They expose aggregate candidate/evaluation/rejection counts, both movement directions, trial/budget exhaustion, and vectors. They never serialize fixed-anchor identifiers or assignment snapshots. Counts include repeated evaluations, not just unique feasible candidates. The initial construction's counters are separate from the two nested construction restarts; restart objective vectors are reported explicitly. Reusing the diagnostic object resets per-generation counters, while preserving supplied anchor constraints. Normal versus instrumented exact-output parity is tested.

## H. Verification and reproducibility

Final executed results and benchmark measurements follow below. No unavailable frontend/API check is inferred from backend success. The current audit changes only generator internals, tests, harness, and this report; the prior settings/API/frontend implementation remains in the working tree. Applicable mocked browser regressions were rerun. Mocked browser tests do not establish production generation or physical-device behavior.

The required commands use isolated output paths to avoid running stale or locked assemblies:

```powershell
dotnet restore GF3.sln
dotnet build GF3.sln --no-restore -p:OutputPath=bin/DeepAuditFinalV2/net10.0/
dotnet test GF3.Tests/GF3.Tests.csproj --no-build --no-restore `
  -p:OutputPath=bin/DeepAuditFinalV2/net10.0/ --filter 'Category!=GenerationStress'
dotnet test GF3.Tests/GF3.Tests.csproj --no-build --no-restore `
  -p:OutputPath=bin/DeepAuditFinalV2/net10.0/ --filter 'Category=GenerationStress'
dotnet build tools/AlgorithmStressHarness/AlgorithmStressHarness.csproj
```

`xunit.runner.json` in the isolated test output disables test-collection parallelization. It does not disable tests. The full existing 200 legacy and 200 Priority stress seeds remain unchanged. The standard offline CLI uses `--db <private-copy> --all-graphs --repeat 3 --engine legacy|app --variant stored --output <outside-repository-directory>`, plus `first`, `first-two`, and `all` variants on the real roster for each `--priority-availability on|off` policy.

The new offline commands are `--deep-audit <absolute-private-config.json>` and `--deep-audit-control <absolute-private-config.json>`. The private config contains `snapshot`, `reports`, `section`, `rosterSourceGraph`, `targetGroup`, and `targetEmployee`; it must remain outside committed artifacts. Sections are `matrix`, `screenshot`, `witnesses`, `real`, `real-base`, and `benchmark` (base case plus observed six-Priority/all-Priority/zero-target timing outliers). Output directories are guarded by the existing replay path checks. The control executable uses the frozen original BusinessLogicLayer assembly; its assembly hash is verified in each output. No source paths or personal selectors are hard-coded in the harness.

Files changed by this audit: `BusinessLogicLayer/Generators/PriorityScheduleGenerator.cs`, `BusinessLogicLayer/Generators/PriorityGenerationAudit.cs`, `BusinessLogicLayer/Properties/AssemblyInfo.cs`, `GF3.Tests/PrioritySchedulingDeepAuditTests.cs`, `GF3.Tests/Infrastructure/PriorityAuditScenarios.cs`, and the harness's `AlgorithmStressHarness.csproj`, `Program.cs`, `PriorityDeepAudit.cs`, `SqliteReplaySource.cs` (partial declaration), plus this report. No public API, schema, migration, frontend, or legacy-generator change was added by this audit.

## I. Remaining limitations

- The saved F35 November graph, its Priority flags, and screenshot images were unavailable. Exact saved/screenshot output cannot be certified; the reconstruction and synthetic input are labeled separately.
- Large monthly results remain heuristic. Two restarts and 16 paired trials are deliberate bounds, not exhaustive anchor or assignment enumeration. Offline witnesses demonstrate remaining optimization headroom; they must not be presented as hard-constraint failures or global-optimality proof.
- Capacity lower bounds relax coupled staffing/full-day interactions. A deficit above a lower bound is not automatically avoidable; a validated better witness is the stronger evidence.
- Formal infeasibility/optimality classifications in this report are audit evidence. The existing production API and UI contracts are unchanged; no solver certificate or new public classification field was added.
- Orientation fairness is outside the current objective. Defining inconvenient shifts, preferences or rotation entitlements requires a business policy; an arbitrary balancing rule was not added.
- ID-based ties remain observable. Caller-list determinism does not imply invariance to changing the identifiers themselves.
- Optional diagnostics add work and are measured separately. Allocation totals are not memory peaks. Controlled timing is compared only under matching warmup/policy conditions; noisy concurrent runs are not used to assert an isolated speed ratio.

## J. Recommendation

Keep the four bounded corrections and their regressions. The original all-morning reconstruction was **valid but demonstrably suboptimal**, so dismissing it as an inevitable consequence of availability would be incorrect. The two-hour Priority shortage is genuinely unavoidable. The corrected schedule is measurably better for the complete team while remaining morning-heavy; that distribution alone does not justify more balancing changes. Priority hour fairness also improves through valid ownership changes. If substantially stronger monthly optimization is required, the retained counterfactual witnesses provide concrete quality/performance targets for a separately scoped search-budget change. Do not market the current bounded generator as globally optimal or as enforcing early/late fairness.

## Measurement appendix

Final generator SHA-256: `E69B33B7812C2EB736B34FE1F6113814CA15C0D378716CE27516B15A0185448F`. All final backend, matrix and replay results use this same generator assembly. The frozen control remains `5DE04294D1C9E09736FEBF7A8E4570B0AEF6811C74C4E917F5D292013630A159`.

### Executed validation

| Check | Final result |
| --- | --- |
| Restore / solution / harness builds | Passed; zero build errors and warnings |
| Ordinary backend | **1,480 passed**, zero failed/skipped; 14m37s; all 112 new audit cases included |
| Fixed stress seeds | **400 passed**, zero failed/skipped; 14m39s; unchanged 200 legacy + 200 Priority seeds, three repeated generations each |
| Frontend tests | **1,002 passed**, 110 files; 86.93s |
| Frontend lint / production build | Passed; zero lint errors, 16 existing warnings; existing SignalR annotation and large-chunk build warnings |
| Priority browser regressions | **8 passed**, four browser/device projects, mocked API; 1.4m |
| Independent tiny references | Three targets, 155 visited states / 36 full feasible states each, exact seven-component equality |
| Safe standard SQLite replay | **70 cases / 210 generations**, zero failures/skips, stable repeats |
| Source protection | Main/WAL/SHM size, SHA-256 and modification time unchanged; fresh-copy integrity `ok`; private source copies removed |

The final matrix has **2 improved / 42 equal-quality cases / zero meaningful regressions**, with 42/44 exact hashes preserved. All ten zero-Priority rows retain exact hashes. The calendar row reduces Priority deficit 1,440→720 and gap 63,900→63,180 minutes. The ownership row preserves raw deficits and coverage while normalized deficit improves 24.0173130→23.7282184; its lower-priority streak penalty rises 23→29. Per-worker orientation sums agree with aggregate counts for every operating template.

Standard replay covers all 32 graphs with both engines (192 generations) plus six first/first-two/all Priority policy variants (18 generations). All 32 stored zero-Priority hashes match app versus legacy exactly. Every run preserves its private source-copy fingerprint and reports `quick_check=ok` before/after. Historical rows remain comparison data. The final ordinary suite also includes 8 settings-controller checks, 25 container-service checks, 316 existing ordinary Priority-generator checks, and eight new persisted-settings routing checks. Unknown deep-audit sections are explicitly refused; that negative CLI check passed.

### Controlled timing and allocations

These eight-case control/candidate runs execute sequentially after this audit’s other test/replay jobs finish. Each case/version receives one warmup and three measured **normal** generations. Optional diagnostics run separately and must match exact normal output. Allocated MiB are cumulative allocation, not peak live heap.

| Case / respect policy | Before ms | After ms | Ratio | Allocated MiB before / after | Vector |
| --- | ---: | ---: | ---: | --- | --- |
| Base / True | 3431.8 | 6474.5 | 1.89× | 1695.6 / 2474.1 | Improved |
| Six Priority / True | 1802.0 | 2901.2 | 1.61× | 1429.6 / 1861.4 | Improved |
| All Priority / True | 124.7 | 137.0 | 1.10× | 113.0 / 126.5 | Improved |
| Target zero / True | 3559.1 | 6035.8 | 1.70× | 1735.4 / 2494.4 | Equal |
| Base / False | 3399.1 | 6432.7 | 1.89× | 1695.6 / 2474.1 | Improved |
| Six Priority / False | 1600.0 | 2487.1 | 1.55× | 1410.0 / 1807.5 | Improved |
| All Priority / False | 281.0 | 278.8 | 0.99× | 251.8 / 257.9 | Improved |
| Target zero / False | 3387.4 | 6212.7 | 1.83× | 1735.4 / 2494.4 | Equal |

The isolated recheck has **0 of eight cases above 2×**. Earlier concurrent runs included ratios above 2× and prompted this recheck. The additional cost is bounded reconstruction: legacy guidance is reused, but ordinary refill, exchanges, anchor search and validation can run twice more. The two zero-target cases retain equal quality while costing 1.70×/1.83×; the six-Priority cases improve quality at 1.61×/1.55×. Ownership checks add bounded comparison work when several Priority workers compete. This is an explicit quality/runtime tradeoff; no latency SLA was supplied, and all measured normal generations complete inside the 30-second guard. The added search cost remains a practical limitation.

Base respect True: before hash `9C5A76A55197C5801B56FCB089182D6D52F010737474C9EE0ABA726C4E24C0BC`; after `A5A2EEE6700E1BC1E00C1C9599E1F6AE4B766C11C3D458B27890B253B5872927`. Exact repeats and diagnostic/plain parity hold.
Base respect False: before hash `9C5A76A55197C5801B56FCB089182D6D52F010737474C9EE0ABA726C4E24C0BC`; after `A5A2EEE6700E1BC1E00C1C9599E1F6AE4B766C11C3D458B27890B253B5872927`. Exact repeats and diagnostic/plain parity hold.

The frozen control lacks the added diagnostic instrumentation: its new rejection counts are **NOT MEASURED**, not zero. Initial-construction candidate diagnostics: 21 early and 21 late generated candidates; 231 feasible early and 327 feasible late evaluation events; single-date 64 trials / 1,720 validations; coordinated 16 trials / 836 validations. The trial limit is reached, neither validation budget is exhausted, and no initial-construction orientation trial is accepted. The two accepted cached-guidance restart vectors are in section C. Rejection events: availability 10,092, consecutive days 1,322, staffing 21, overlap/position 3,583, full-day/position limit 1,862; absent categories mean zero observed events. Six externally blocked dates and nine weekend-blocked dates are separate counts. Nested restart validation totals are **NOT MEASURED** by these initial-construction counters; their per-construction bounds still apply.

### Complete-month counterfactuals

| Respect | Feasible single flips | Better / equal / worse | Feasible pairs | Better / equal / worse |
| --- | ---: | --- | ---: | --- |
| True | 21 | 7/3/11 | 15 | 4/1/10 |
| False | 21 | 7/3/11 | 15 | 4/1/10 |

A remaining feasible late anchor on date 10 yields `[120,4260,17160,0.5823153000705181,7.2078685614669435,177,11]`, with 18 early / 3 late shifts and zero hard violations: **330 additional coverage minutes and 30 fewer ordinary deficit minutes** than the final base. One tested two-date constraint reaches the same vector. Lower-priority utilization/rest penalties rise. Thus the final base is explicitly **VALID BUT SUBOPTIMAL relative to this witness**. Production tests two earliest eligible restart anchors; the larger offline sweep explores more dates. The three deterministic monthly regressions retain this defect class without committing personal availability.

The 112 real cases comprise two bases, 42 single flips, 30 pairs, 14 Priority-count variants, 18 target variants, two reversals and four ID rotations. All requested anchors in these real trials are realized and independently valid. Normal cases have three measured repeats plus warmup and separate diagnostics; constrained diagnostic cases have one measured run without warmup. Constrained-case times are not like-for-like normal-generation benchmarks.

### Priority count and target patterns

| Priority count | ON early/late | OFF early/late | ON / OFF Priority deficit minutes |
| ---: | --- | --- | --- |
| 0 | 0/0 | 0/0 | 0 / 0 |
| 1 | 19/2 | 19/2 | 120 / 120 |
| 2 | 38/2 | 41/1 | 2400 / 1440 |
| 3 | 54/4 | 60/3 | 5160 / 2760 |
| 4 | 72/5 | 78/6 | 7440 / 4080 |
| 6 | 76/6 | 81/3 | 17160 / 16200 |
| 12 | 63/21 | 84/0 | 56700 / 56700 |

All-Priority normalized deficit: ON 10.7063576258→8.6492869707; OFF 3.9336433588→3.8872729884. Raw deficit 56,700 and gap 46,080 minutes are preserved. The final OFF counts exactly match the independent rational hours-reference optimum: **OPTIMAL WITHIN REFERENCE SPACE** for that prefix. This does not certify rest-date optimization or orientation fairness.
There is no monotone count/early-share relationship. The base ON/OFF outputs coincide, while all-Priority ON has 63/21 and OFF 84/0; removing availability restrictions changes eligibility and orientation options together. With no ordinary workers, equal-duration fixed-date orientation changes alone cannot improve the objective. A minimum is not a cap: filling residual demand above a small target is valid.

| Target h | Actual Priority h ON / OFF | ON early/late | OFF early/late | ON / OFF deficit min |
| ---: | --- | --- | --- | --- |
| 0 | 72 / 72 | 9/0 | 9/0 | 0 / 0 |
| 40 | 96 / 96 | 12/0 | 12/0 | 0 / 0 |
| 80 | 120 / 120 | 15/0 | 15/0 | 0 / 0 |
| 120 | 136 / 136 | 16/1 | 16/1 | 0 / 0 |
| 160 | 168 / 168 | 19/2 | 19/2 | 0 / 0 |
| 168 | 168 / 168 | 19/2 | 19/2 | 0 / 0 |
| 170 | 168 / 168 | 19/2 | 19/2 | 120 / 120 |
| 180 | 168 / 168 | 19/2 | 19/2 | 720 / 720 |
| 200 | 168 / 168 | 19/2 | 19/2 | 1920 / 1920 |

At high targets the 168-hour weekday limit dominates. Targets also change reservation and ordinary-month allocation; they do not create an orientation preference in the objective.

### Ordering and identifier sensitivity

| Respect | Input | Gap | Ordinary deficit | Normalized deficit |
| --- | --- | ---: | ---: | ---: |
| True | Canonical | 4590 | 17190 | 0.583580732169284 |
| True | ID rotation one | 4740 | 17160 | 0.583482920876920 |
| True | ID rotation last | 4590 | 17190 | 0.583580732169284 |
| False | Canonical | 4590 | 17190 | 0.583580732169284 |
| False | ID rotation one | 4740 | 17160 | 0.583482920876920 |
| False | ID rotation last | 4590 | 17190 | 0.583580732169284 |

Both real input reversals preserve exact canonical hashes. Remapped IDs preserve eligibility and external constraints but can change meaningful heuristic quality; hashes naturally differ when identifiers themselves change. Identical inputs remain deterministic. Floating-point-only changes are not counted as meaningful improvements.

Final classifications: zero **HARD VIOLATION** results; three tiny full-vector **OPTIMAL WITHIN REFERENCE SPACE** results; proven infeasible weekday hours above 168 and the all-Priority full-coverage shortage; documented **VALID BUT SUBOPTIMAL** monthly witnesses; other unproved monthly optimality/feasibility claims remain **PARTIAL — FEASIBILITY NOT PROVEN**. No completed independent reference exhausted its bound. Stress passes are not global-optimality certificates.

