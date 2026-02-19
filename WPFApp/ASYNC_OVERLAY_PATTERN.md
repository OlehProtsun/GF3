# Async Working/Success Overlay Pattern

Use this pattern for async Open/Edit/AddNew/Save/Delete/navigation actions in module root view-models.

## Required flow

1. `var uiToken = ResetNavUiCts(ct)` to cancel prior UI-status sequences.
2. `await ShowNavWorkingAsync()` to show global overlay (`Working...`).
3. `await Dispatcher.InvokeAsync(..., DispatcherPriority.ApplicationIdle)` so Working is rendered before heavy work/navigation.
4. Run async business logic and switch section/view (`SwitchToXxxAsync`).
5. `await Dispatcher.InvokeAsync(..., DispatcherPriority.ApplicationIdle)` after navigation so target view renders first.
6. `await ShowNavSuccessThenAutoHideAsync(uiToken, delayMs)` for brief Success state, then auto-hide.

## Errors / cancellation

- `OperationCanceledException`: hide overlay (`HideNavStatusAsync`) and return.
- Any other exception: hide overlay, then show existing error dialog flow.

## Threading

- Any UI-bound property/collection updates must happen on UI thread via `RunOnUiThreadAsync` (or Dispatcher checks).
- If background awaits use `ConfigureAwait(false)`, dispatch back before touching UI-bound state.

## XAML placement

- Overlay (`dim Border + UIStatus`) belongs in module shell/root view that hosts section navigation (`ContentControl`).
- Avoid per-subview overlays that disappear during section switches.

## Delay recommendations

- Fast tab/section switch/open editor: ~500–700 ms.
- Navigation + save/delete + reload flows: ~700–900 ms.
