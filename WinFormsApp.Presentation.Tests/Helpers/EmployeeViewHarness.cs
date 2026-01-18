using DataAccessLayer.Models;
using Moq;
using System;
using System.Threading;
using System.Windows.Forms;
using WinFormsApp.View.Employee;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presentation.Tests.Helpers;

internal sealed class EmployeeViewHarness
{
    public Mock<IEmployeeView> Mock { get; }
    public BindingSource BindingSource { get; private set; } = new();

    public Func<CancellationToken, Task>? SearchHandler { get; private set; }
    public Func<CancellationToken, Task>? AddHandler { get; private set; }
    public Func<CancellationToken, Task>? EditHandler { get; private set; }
    public Func<CancellationToken, Task>? DeleteHandler { get; private set; }
    public Func<CancellationToken, Task>? SaveHandler { get; private set; }
    public Func<CancellationToken, Task>? CancelHandler { get; private set; }
    public Func<CancellationToken, Task>? OpenProfileHandler { get; private set; }

    public EmployeeViewHarness(CancellationToken lifetimeToken = default)
    {
        Mock = new Mock<IEmployeeView>(MockBehavior.Strict);
        Mock.SetupAllProperties();
        Mock.SetupGet(v => v.LifetimeToken).Returns(lifetimeToken);

        Mock.Setup(v => v.RunBusyAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken, string?>((action, ct, _) => action(ct));

        Mock.Setup(v => v.RunBusyAsync(It.IsAny<Func<CancellationToken, IProgress<int>?, Task>>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Returns<Func<CancellationToken, IProgress<int>?, Task>, CancellationToken, string?>((action, ct, _) => action(ct, null));

        Mock.Setup(v => v.SetEmployeeListBindingSource(It.IsAny<BindingSource>()))
            .Callback<BindingSource>(bs => BindingSource = bs);

        Mock.Setup(v => v.ClearInputs());
        Mock.Setup(v => v.ClearValidationErrors());
        Mock.Setup(v => v.SetValidationErrors(It.IsAny<IReadOnlyDictionary<string, string>>()));
        Mock.Setup(v => v.SwitchToEditMode());
        Mock.Setup(v => v.SwitchToListMode());
        Mock.Setup(v => v.SwitchToProfileMode());
        Mock.Setup(v => v.SetProfile(It.IsAny<EmployeeModel>()));
        Mock.Setup(v => v.ShowInfo(It.IsAny<string>()));
        Mock.Setup(v => v.ShowError(It.IsAny<string>()));
        Mock.Setup(v => v.Confirm(It.IsAny<string>(), It.IsAny<string?>())).Returns(false);

        Mock.SetupAdd(v => v.SearchEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => SearchHandler += handler);
        Mock.SetupAdd(v => v.AddEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => AddHandler += handler);
        Mock.SetupAdd(v => v.EditEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => EditHandler += handler);
        Mock.SetupAdd(v => v.DeleteEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => DeleteHandler += handler);
        Mock.SetupAdd(v => v.SaveEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => SaveHandler += handler);
        Mock.SetupAdd(v => v.CancelEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => CancelHandler += handler);
        Mock.SetupAdd(v => v.OpenProfileEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => OpenProfileHandler += handler);
    }

    public Task RaiseSearchAsync(CancellationToken ct = default)
        => SearchHandler?.Invoke(ct) ?? throw new InvalidOperationException("Search handler not registered.");

    public Task RaiseAddAsync(CancellationToken ct = default)
        => AddHandler?.Invoke(ct) ?? throw new InvalidOperationException("Add handler not registered.");

    public Task RaiseEditAsync(CancellationToken ct = default)
        => EditHandler?.Invoke(ct) ?? throw new InvalidOperationException("Edit handler not registered.");

    public Task RaiseDeleteAsync(CancellationToken ct = default)
        => DeleteHandler?.Invoke(ct) ?? throw new InvalidOperationException("Delete handler not registered.");

    public Task RaiseSaveAsync(CancellationToken ct = default)
        => SaveHandler?.Invoke(ct) ?? throw new InvalidOperationException("Save handler not registered.");

    public Task RaiseCancelAsync(CancellationToken ct = default)
        => CancelHandler?.Invoke(ct) ?? throw new InvalidOperationException("Cancel handler not registered.");

    public Task RaiseOpenProfileAsync(CancellationToken ct = default)
        => OpenProfileHandler?.Invoke(ct) ?? throw new InvalidOperationException("OpenProfile handler not registered.");
}
