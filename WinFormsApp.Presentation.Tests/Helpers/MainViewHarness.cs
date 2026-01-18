using Moq;
using System;
using System.Threading;
using WinFormsApp.View.Main;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presentation.Tests.Helpers;

internal sealed class MainViewHarness
{
    public Mock<IMainView> Mock { get; }

    public Func<CancellationToken, Task>? ShowEmployeeHandler { get; private set; }
    public Func<CancellationToken, Task>? ShowAvailabilityHandler { get; private set; }
    public Func<CancellationToken, Task>? ShowContainerHandler { get; private set; }

    public NavPage ActivePage { get; private set; }

    public MainViewHarness(CancellationToken lifetimeToken = default)
    {
        Mock = new Mock<IMainView>(MockBehavior.Strict);
        Mock.SetupGet(v => v.LifetimeToken).Returns(lifetimeToken);
        Mock.Setup(v => v.RunBusyAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken, string?>((action, ct, _) =>
            {
                try
                {
                    return action(ct);
                }
                catch (OperationCanceledException)
                {
                    return Task.CompletedTask;
                }
            });
        Mock.Setup(v => v.RunBusyAsync(It.IsAny<Func<CancellationToken, IProgress<int>?, Task>>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Returns<Func<CancellationToken, IProgress<int>?, Task>, CancellationToken, string?>((action, ct, _) =>
            {
                try
                {
                    return action(ct, null);
                }
                catch (OperationCanceledException)
                {
                    return Task.CompletedTask;
                }
            });
        Mock.Setup(v => v.SetActivePage(It.IsAny<NavPage>()))
            .Callback<NavPage>(page => ActivePage = page);
        Mock.SetupGet(v => v.ActivePage).Returns(() => ActivePage);
        Mock.Setup(v => v.BeginWindowDrag());

        Mock.SetupAdd(v => v.ShowEmployeeView += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ShowEmployeeHandler += handler);
        Mock.SetupAdd(v => v.ShowAvailabilityView += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ShowAvailabilityHandler += handler);
        Mock.SetupAdd(v => v.ShowContainerView += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ShowContainerHandler += handler);
    }

    public Task RaiseShowEmployeeAsync(CancellationToken ct = default)
        => ShowEmployeeHandler?.Invoke(ct) ?? throw new InvalidOperationException("ShowEmployee handler not registered.");

    public Task RaiseShowAvailabilityAsync(CancellationToken ct = default)
        => ShowAvailabilityHandler?.Invoke(ct) ?? throw new InvalidOperationException("ShowAvailability handler not registered.");

    public Task RaiseShowContainerAsync(CancellationToken ct = default)
        => ShowContainerHandler?.Invoke(ct) ?? throw new InvalidOperationException("ShowContainer handler not registered.");
}
