# WPF Performance Baseline (Phase 0)

## Scope reviewed
- `WPFApp/View/**` (Home, Availability, Container/Schedule, Database)
- `WPFApp/Resources/Styles/DataGrids.xaml`
- `WPFApp/ViewModel/**` flow methods for list/profile/edit/save

## Potential bottlenecks found
1. **Virtualization is not consistently enforced across all grids**
   - Base DataGrid style did not explicitly force full virtualization/recycling flags.
   - Some heavy matrix grids rely on per-view code-behind tuning, but style-level defaults were missing.

2. **Deferred scrolling disabled in heavy matrix grids**
   - `ScrollViewer.IsDeferredScrollingEnabled` was explicitly set to `false` in schedule/profile/home matrix tuning helpers.
   - For large row/column matrices this can increase layout churn while dragging scrollbar thumb.

3. **Potential UI-thread sync dispatch in matrix change handlers**
   - `Dispatcher.Invoke` is used in availability matrix update handlers.
   - Sync invoke can stall caller/background worker and increase perceived lag under frequent updates.

4. **No unified smooth wheel scrolling behavior for touchpad/mouse wheel**
   - Multiple scroll-heavy screens rely only on default wheel behavior.
   - Precision/touchpad scroll experience can be uneven in nested grid/scroll viewer scenarios.

5. **Limited explicit perf telemetry for key UI flows**
   - Logger exists, but key flows (search/open/save/load) lacked consistent lightweight elapsed-time markers.

## Highest-risk screens / controls
- **Container Schedule Edit/Profile** (matrix-like grids, synchronized scrolling, many cells)
- **Home matrix card grids**
- **Availability Edit/Profile matrix DataGrid**
- **Large list DataGrids** (Container/Employee/Availability lists)
- **Database query result grid** (auto-generated columns + potentially large result sets)

## Baseline instrumentation plan
- Add lightweight `PerfScope` (`IDisposable + Stopwatch`) with `Debug.WriteLine` + existing `ILoggerService.LogPerf`.
- Instrument key flows:
  - Home load (`LoadDataAsync`)
  - Availability flows (`Search`, `OpenProfile`, `EditSelected`, `Save`, `Delete`)
- Keep instrumentation non-invasive (no UI changes, no blocking, no behavior side effects).
