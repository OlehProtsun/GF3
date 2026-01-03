using DataAccessLayer.Models;
using Moq;
using System.Threading;
using System.Windows.Forms;
using WinFormsApp.View.Container;

namespace WinFormsApp.Presentation.Tests.Helpers;

internal sealed class ContainerViewHarness
{
    public Mock<IContainerView> Mock { get; }
    public BindingSource ContainerBinding { get; private set; } = new();
    public BindingSource ScheduleBinding { get; private set; } = new();
    public BindingSource SlotBinding { get; private set; } = new();

    public Func<CancellationToken, Task>? SearchHandler { get; private set; }
    public Func<CancellationToken, Task>? AddHandler { get; private set; }
    public Func<CancellationToken, Task>? EditHandler { get; private set; }
    public Func<CancellationToken, Task>? DeleteHandler { get; private set; }
    public Func<CancellationToken, Task>? SaveHandler { get; private set; }
    public Func<CancellationToken, Task>? CancelHandler { get; private set; }
    public Func<CancellationToken, Task>? OpenProfileHandler { get; private set; }

    public Func<CancellationToken, Task>? ScheduleSearchHandler { get; private set; }
    public Func<CancellationToken, Task>? ScheduleAddHandler { get; private set; }
    public Func<CancellationToken, Task>? ScheduleEditHandler { get; private set; }
    public Func<CancellationToken, Task>? ScheduleDeleteHandler { get; private set; }
    public Func<CancellationToken, Task>? ScheduleSaveHandler { get; private set; }
    public Func<CancellationToken, Task>? ScheduleCancelHandler { get; private set; }
    public Func<CancellationToken, Task>? ScheduleOpenProfileHandler { get; private set; }
    public Func<CancellationToken, Task>? ScheduleGenerateHandler { get; private set; }

    public ContainerViewHarness(CancellationToken lifetimeToken = default)
    {
        Mock = new Mock<IContainerView>(MockBehavior.Strict);
        Mock.SetupAllProperties();
        Mock.SetupGet(v => v.LifetimeToken).Returns(lifetimeToken);

        Mock.Setup(v => v.RunBusyAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>(), It.IsAny<string?>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken, string?>((action, ct, _) => action(ct));

        Mock.Setup(v => v.SetContainerBindingSource(It.IsAny<BindingSource>()))
            .Callback<BindingSource>(bs => ContainerBinding = bs);
        Mock.Setup(v => v.SetScheduleBindingSource(It.IsAny<BindingSource>()))
            .Callback<BindingSource>(bs => ScheduleBinding = bs);
        Mock.Setup(v => v.SetSlotBindingSource(It.IsAny<BindingSource>()))
            .Callback<BindingSource>(bs => SlotBinding = bs);

        Mock.Setup(v => v.SetAvailabilityGroupList(It.IsAny<IEnumerable<AvailabilityGroupModel>>()));
        Mock.Setup(v => v.ClearInputs());
        Mock.Setup(v => v.ClearScheduleInputs());
        Mock.Setup(v => v.ClearValidationErrors());
        Mock.Setup(v => v.ClearScheduleValidationErrors());
        Mock.Setup(v => v.SetValidationErrors(It.IsAny<IReadOnlyDictionary<string, string>>()));
        Mock.Setup(v => v.SetScheduleValidationErrors(It.IsAny<IReadOnlyDictionary<string, string>>()));
        Mock.Setup(v => v.SwitchToEditMode());
        Mock.Setup(v => v.SwitchToListMode());
        Mock.Setup(v => v.SwitchToProfileMode());
        Mock.Setup(v => v.SwitchToScheduleEditMode());
        Mock.Setup(v => v.SwitchToScheduleListMode());
        Mock.Setup(v => v.SwitchToScheduleProfileMode());
        Mock.Setup(v => v.SetProfile(It.IsAny<ContainerModel>()));
        Mock.Setup(v => v.SetScheduleProfile(It.IsAny<ScheduleModel>()));
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

        Mock.SetupAdd(v => v.ScheduleSearchEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ScheduleSearchHandler += handler);
        Mock.SetupAdd(v => v.ScheduleAddEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ScheduleAddHandler += handler);
        Mock.SetupAdd(v => v.ScheduleEditEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ScheduleEditHandler += handler);
        Mock.SetupAdd(v => v.ScheduleDeleteEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ScheduleDeleteHandler += handler);
        Mock.SetupAdd(v => v.ScheduleSaveEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ScheduleSaveHandler += handler);
        Mock.SetupAdd(v => v.ScheduleCancelEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ScheduleCancelHandler += handler);
        Mock.SetupAdd(v => v.ScheduleOpenProfileEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ScheduleOpenProfileHandler += handler);
        Mock.SetupAdd(v => v.ScheduleGenerateEvent += It.IsAny<Func<CancellationToken, Task>>())
            .Callback<Func<CancellationToken, Task>>(handler => ScheduleGenerateHandler += handler);
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

    public Task RaiseScheduleSearchAsync(CancellationToken ct = default)
        => ScheduleSearchHandler?.Invoke(ct) ?? throw new InvalidOperationException("ScheduleSearch handler not registered.");

    public Task RaiseScheduleAddAsync(CancellationToken ct = default)
        => ScheduleAddHandler?.Invoke(ct) ?? throw new InvalidOperationException("ScheduleAdd handler not registered.");

    public Task RaiseScheduleEditAsync(CancellationToken ct = default)
        => ScheduleEditHandler?.Invoke(ct) ?? throw new InvalidOperationException("ScheduleEdit handler not registered.");

    public Task RaiseScheduleDeleteAsync(CancellationToken ct = default)
        => ScheduleDeleteHandler?.Invoke(ct) ?? throw new InvalidOperationException("ScheduleDelete handler not registered.");

    public Task RaiseScheduleSaveAsync(CancellationToken ct = default)
        => ScheduleSaveHandler?.Invoke(ct) ?? throw new InvalidOperationException("ScheduleSave handler not registered.");

    public Task RaiseScheduleCancelAsync(CancellationToken ct = default)
        => ScheduleCancelHandler?.Invoke(ct) ?? throw new InvalidOperationException("ScheduleCancel handler not registered.");

    public Task RaiseScheduleOpenProfileAsync(CancellationToken ct = default)
        => ScheduleOpenProfileHandler?.Invoke(ct) ?? throw new InvalidOperationException("ScheduleOpenProfile handler not registered.");

    public Task RaiseScheduleGenerateAsync(CancellationToken ct = default)
        => ScheduleGenerateHandler?.Invoke(ct) ?? throw new InvalidOperationException("ScheduleGenerate handler not registered.");
}
