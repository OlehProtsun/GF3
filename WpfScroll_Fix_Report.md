# WPF Scroll Fix Report

## Root cause

Під час аудиту виявлено кілька точок, які разом давали ефект "drag-scroll only on release" і загальну в'ялість скролу:

1. `ScrollViewer.IsDeferredScrollingEnabled=True` у базових стилях DataGrid:
   - `WPFApp/Resources/Styles/DataGrids.xaml` (AvailabilityBaseGridStyle).
   - `WPFApp/Resources/Styles/DataGrids.xaml` (ScheduleMatrixGridStyle).
2. Додатково `IsDeferredScrollingEnabled=True` виставлявся programmatic у code-behind:
   - `WPFApp/View/Container/ContainerScheduleEditView.xaml.cs` (`ConfigureGridPerformance`).
   - `WPFApp/View/Container/ContainerScheduleProfileView.xaml.cs` (`ConfigureGridPerformance`).
   - `WPFApp/View/Home/HomeView.xaml.cs` (`ConfigureGridPerformance`).
3. У стилі був увімкнений custom wheel-interceptor:
   - `helpers:SmoothScrollBehavior.IsEnabled=True` у `WPFApp/Resources/Styles/DataGrids.xaml`.
   - Реалізація в `WPFApp/UI/Helpers/SmoothScrollBehavior.cs` перехоплювала `PreviewMouseWheel`, анімувала offset і ставила `e.Handled=true`.
4. Для virtualized grids використовувався `VirtualizingPanel.ScrollUnit="Item"`, що дає більш "ступінчасту" прокрутку (особливо на touchpad/wheel):
   - `WPFApp/Resources/Styles/DataGrids.xaml`.
   - `WPFApp/View/Container/ContainerScheduleEditView.xaml.cs`.
   - `WPFApp/View/Container/ContainerScheduleProfileView.xaml.cs`.
   - `WPFApp/View/Home/HomeView.xaml.cs`.
5. Діагностичний scroll handler в schedule edit view (`ScrollChanged`) був підписаний на routed event і робив perf-логування, створюючи зайву роботу під час активного scroll:
   - `WPFApp/View/Container/ContainerScheduleEditView.xaml.cs`.

## Що змінено

- Вимкнено deferred scrolling у стилях/коді (скрізь `False`).
- Видалено `SmoothScrollBehavior` і його підключення у стилях.
- Переведено scroll-unit для virtualized grids на `Pixel` при збереженні virtualization+recycling.
- Збережено virtualization (`CanContentScroll=True`, `VirtualizationMode=Recycling`, row/column virtualization увімкнені).
- Для scroll-heavy matrix refresh залишено coalescing і додано throttled dispatch (~16ms) для зменшення UI-thread backlog:
  - `ContainerScheduleEditView.xaml.cs` (throttle timer + coalesced refresh).
  - `ContainerScheduleProfileView.xaml.cs` (throttle timer + coalesced refresh).
- Видалено scroll perf logging hook у schedule edit view.

## Manual verification checklist

- [ ] Drag scrollbar thumb: контент рухається під час drag (не після release).
- [ ] Wheel-scroll: реактивний, без відкладеної/інерційної затримки.
- [ ] Precision touchpad: плавний, без фризів.
- [ ] Keyboard navigation: Up/Down/PageUp/PageDown/Home/End працюють як раніше.
- [ ] Virtualization активна (rows/columns не рендеряться повністю upfront, контейнери рецикляться).
