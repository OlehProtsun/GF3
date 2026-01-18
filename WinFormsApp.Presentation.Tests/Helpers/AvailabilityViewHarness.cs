using DataAccessLayer.Models;
using Moq;
using System;
using System.Threading;
using System.Windows.Forms;
using WinFormsApp.View.Availability;

namespace WinFormsApp.Presentation.Tests.Helpers;

internal sealed class AvailabilityViewHarness
{
    public Mock<IAvailabilityView> Mock { get; }
    public BindingSource ListBindingSource { get; private set; } = new();
    public BindingSource BindsBindingSource { get; private set; } = new();

    public Func<CancellationToken, Task>? SearchHandler { get; private set; }
    public Func<CancellationToken, Task>? AddHandler { get; private set; }
    public Func<CancellationToken, Task>? EditHandler { get; private set; }
    public Func<CancellationToken, Task>? DeleteHandler { get; private set; }
    public Func<CancellationToken, Task>? SaveHandler { get; private set; }
    public Func<CancellationToken, Task>? CancelHandler { get; private set; }
    public Func<CancellationToken, Task>? OpenProfileHandler { get; private set; }
    public Func<CancellationToken, Task>? AddBindHandler { get; private set; }
    public Func<BindModel, CancellationToken, Task>? UpsertBindHandler { get; private set; }
    public Func<BindModel, CancellationToken, Task>? DeleteBindHandler { get; private set; }
    public Func<CancellationToken, Task>? AddEmployeeHandler { get; private set; }
    public Func<CancellationToken, Task>? RemoveEmployeeHandler { get; private set; }

    public AvailabilityViewHarness(CancellationToken lifetimeToken = default)
    {
        Mock = new Mock<IAvailabilityView>(MockBehavior.Strict);
        Mock.SetupAllProperties();
        Mock.SetupGet(v => v.LifetimeToken).Returns(lifetimeToken);

        Mock.Setup(v => v.RunBusyAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken, string?>((action, ct, _) => action(ct));

        Mock.Setup(v => v.RunBusyAsync(It.IsAny<Func<CancellationToken, IProgress<int>?, Task>>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Returns<Func<CancellationToken, IProgress<int>?, Task>, CancellationToken, string?>((action, ct, _) => action(ct, null));

        Mock.Setup(v => v.SetListBindingSource(It.IsAny<BindingSource>()))
            .Callback<BindingSource>(bs => ListBindingSource = bs);
        Mock.Setup(v => v.SetBindsBindingSource(It.IsAny<BindingSource>()))
            .Callback<BindingSource>(bs => BindsBindingSource = bs);

        Mock.Setup(v => v.ClearInputs());
        Mock.Setup(v => v.ClearValidationErrors());
        Mock.Setup(v => v.SetValidationErrors(It.IsAny<IReadOnlyDictionary<string, string>>()));
        Mock.Setup(v => v.SwitchToEditMode());
        Mock.Setup(v => v.SwitchToListMode());
        Mock.Setup(v => v.SwitchToProfileMode());
        Mock.Setup(v => v.ShowInfo(It.IsAny<string>()));
        Mock.Setup(v => v.ShowError(It.IsAny<string>()));
        Mock.Setup(v => v.ResetGroupMatrix());
        Mock.Setup(v => v.TryAddEmployeeColumn(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
        Mock.Setup(v => v.RemoveEmployeeColumn(It.IsAny<int>())).Returns(true);
        Mock.Setup(v => v.GetSelectedEmployeeIds()).Returns(Array.Empty<int>());
        Mock.Setup(v => v.ReadGroupCodes()).Returns(Array.Empty<(int employeeId, IList<(int dayOfMonth, string code)> codes)>());
        Mock.Setup(v => v.SetEmployeeCodes(It.IsAny<int>(), It.IsAny<IList<(int dayOfMonth, string code)>>()));
        Mock.Setup(v => v.SetProfile(It.IsAny<AvailabilityGroupModel>(), It.IsAny<List<AvailabilityGroupMemberModel>>(), It.IsAny<List<AvailabilityGroupDayModel>>()));
        Mock.Setup(v => v.SetEmployeeList(It.IsAny<IEnumerable<EmployeeModel>>()));
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
        Mock.SetupAdd(v => v.AddBindEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => AddBindHandler += handler);
        Mock.SetupAdd(v => v.UpsertBindEvent += It.IsAny<Func<BindModel, CancellationToken, Task>>())
            .Callback<Func<BindModel, CancellationToken, Task>>(handler => UpsertBindHandler += handler);
        Mock.SetupAdd(v => v.DeleteBindEvent += It.IsAny<Func<BindModel, CancellationToken, Task>>())
            .Callback<Func<BindModel, CancellationToken, Task>>(handler => DeleteBindHandler += handler);
        Mock.SetupAdd(v => v.AddEmployeeToGroupEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => AddEmployeeHandler += handler);
        Mock.SetupAdd(v => v.RemoveEmployeeFromGroupEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => RemoveEmployeeHandler += handler);
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

    public Task RaiseAddBindAsync(CancellationToken ct = default)
        => AddBindHandler?.Invoke(ct) ?? throw new InvalidOperationException("AddBind handler not registered.");

    public Task RaiseUpsertBindAsync(BindModel bind, CancellationToken ct = default)
        => UpsertBindHandler?.Invoke(bind, ct) ?? throw new InvalidOperationException("UpsertBind handler not registered.");

    public Task RaiseDeleteBindAsync(BindModel bind, CancellationToken ct = default)
        => DeleteBindHandler?.Invoke(bind, ct) ?? throw new InvalidOperationException("DeleteBind handler not registered.");

    public Task RaiseAddEmployeeAsync(CancellationToken ct = default)
        => AddEmployeeHandler?.Invoke(ct) ?? throw new InvalidOperationException("AddEmployee handler not registered.");

    public Task RaiseRemoveEmployeeAsync(CancellationToken ct = default)
        => RemoveEmployeeHandler?.Invoke(ct) ?? throw new InvalidOperationException("RemoveEmployee handler not registered.");
}
