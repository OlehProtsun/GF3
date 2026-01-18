using BusinessLogicLayer.Generators;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using FluentAssertions;
using Moq;
using WinFormsApp.Presenter.Container;
using WinFormsApp.Presentation.Tests.Helpers;
using WinFormsApp.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WinFormsApp.Presentation.Tests;

[TestClass]
public class ContainerPresenterTests
{
    [TestMethod]
    public async Task InitializeAsync_LoadsContainersAndLookups()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        var containers = new List<ContainerModel> { ModelBuilder.Container(1) };
        var groups = new List<AvailabilityGroupModel> { ModelBuilder.AvailabilityGroup(1) };

        containerService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(containers);
        availabilityService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(groups);

        var presenter = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await presenter.InitializeAsync();

        view.ContainerBinding.DataSource.Should().BeSameAs(containers);
        view.Mock.Verify(v => v.SetAvailabilityGroupList(groups), Times.Once);
    }

    [TestMethod]
    public async Task Search_UsesGetByValue_WhenTermProvided()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.SearchValue = "north";
        var list = new List<ContainerModel> { ModelBuilder.Container(2) };

        containerService.Setup(s => s.GetByValueAsync("north", It.IsAny<CancellationToken>())).ReturnsAsync(list);

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseSearchAsync();

        view.ContainerBinding.DataSource.Should().BeSameAs(list);
        containerService.Verify(s => s.GetByValueAsync("north", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Search_EmptyTerm_UsesGetAllAsync()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.SearchValue = "";
        var list = new List<ContainerModel> { ModelBuilder.Container(10) };

        containerService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(list);

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseSearchAsync();

        view.ContainerBinding.DataSource.Should().BeSameAs(list);
        containerService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Search_WhenCancelled_DoesNotShowError()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.SearchValue = "term";

        containerService.Setup(s => s.GetByValueAsync("term", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseSearchAsync();

        view.Mock.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Add_ClearsInputsAndSwitchesToEdit()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseAddAsync();

        view.Mock.Object.IsEdit.Should().BeFalse();
        view.Mock.Object.Message.Should().Be("Fill the form and press Save.");
        view.Mock.Object.CancelTarget.Should().Be(ContainerViewModel.List);
        view.Mock.Verify(v => v.ClearValidationErrors(), Times.Once);
        view.Mock.Verify(v => v.ClearInputs(), Times.Once);
        view.Mock.Verify(v => v.SwitchToEditMode(), Times.Once);
    }

    [TestMethod]
    public async Task Edit_PopulatesFieldsAndSwitchesToEdit()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);
        var container = ModelBuilder.Container(3, "Main");

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        view.ContainerBinding.DataSource = new List<ContainerModel> { container };
        view.ContainerBinding.Position = 0;
        view.Mock.Object.Mode = ContainerViewModel.List;

        await view.RaiseEditAsync();

        view.Mock.Object.ContainerId.Should().Be(container.Id);
        view.Mock.Object.ContainerName.Should().Be(container.Name);
        view.Mock.Object.ContainerNote.Should().Be(container.Note);
        view.Mock.Object.IsEdit.Should().BeTrue();
        view.Mock.Object.CancelTarget.Should().Be(ContainerViewModel.List);
        view.Mock.Verify(v => v.SwitchToEditMode(), Times.Once);
    }

    [TestMethod]
    public async Task Save_InvalidContainer_SetsValidationErrors()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.ContainerName = "";

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseSaveAsync();

        view.Mock.Verify(v => v.SetValidationErrors(It.Is<IReadOnlyDictionary<string, string>>(errors => errors.ContainsKey("ContainerName"))), Times.Once);
        containerService.Verify(s => s.CreateAsync(It.IsAny<ContainerModel>(), It.IsAny<CancellationToken>()), Times.Never);
        view.Mock.Object.IsSuccessful.Should().BeFalse();
        view.Mock.Object.Message.Should().Be("Please fix the highlighted fields.");
    }

    [TestMethod]
    public async Task Save_ValidCreate_CallsServiceAndSwitches()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.ContainerName = "Main";
        view.Mock.Object.IsEdit = false;
        view.Mock.Object.CancelTarget = ContainerViewModel.List;

        containerService.Setup(s => s.CreateAsync(It.IsAny<ContainerModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(ModelBuilder.Container(5));
        containerService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ContainerModel>());

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseSaveAsync();

        containerService.Verify(s => s.CreateAsync(It.IsAny<ContainerModel>(), It.IsAny<CancellationToken>()), Times.Once);
        view.Mock.Verify(v => v.ShowInfo("Container added successfully."), Times.Once);
        view.Mock.Verify(v => v.SwitchToListMode(), Times.Once);
        view.Mock.Object.IsSuccessful.Should().BeTrue();
    }

    [TestMethod]
    public async Task Delete_ConfirmTrue_DeletesAndReloads()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);
        var container = ModelBuilder.Container(6, "Main");

        view.ContainerBinding.DataSource = new List<ContainerModel> { container };
        view.ContainerBinding.Position = 0;

        view.Mock.Setup(v => v.Confirm(It.IsAny<string>(), It.IsAny<string?>())).Returns(true);
        containerService.Setup(s => s.DeleteAsync(container.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        containerService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ContainerModel>());

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseDeleteAsync();

        containerService.Verify(s => s.DeleteAsync(container.Id, It.IsAny<CancellationToken>()), Times.Once);
        view.Mock.Verify(v => v.ShowInfo("Container deleted successfully."), Times.Once);
        view.Mock.Verify(v => v.SwitchToListMode(), Times.Once);
    }

    [TestMethod]
    public async Task Cancel_FromEdit_SwitchesToProfileTarget()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.CancelTarget = ContainerViewModel.Profile;

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseCancelAsync();

        view.Mock.Verify(v => v.ClearValidationErrors(), Times.Once);
        view.Mock.Verify(v => v.SwitchToProfileMode(), Times.Once);
    }

    [TestMethod]
    public async Task OpenProfile_LoadsSchedulesAndSwitchesMode()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);
        var container = ModelBuilder.Container(7, "Main");

        view.ContainerBinding.DataSource = new List<ContainerModel> { container };
        view.ContainerBinding.Position = 0;

        scheduleService.Setup(s => s.GetByContainerAsync(container.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleModel>());

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseOpenProfileAsync();

        view.Mock.Verify(v => v.SetProfile(container), Times.Once);
        view.Mock.Verify(v => v.SwitchToProfileMode(), Times.Once);
        view.Mock.Object.CancelTarget.Should().Be(ContainerViewModel.List);
    }

    [TestMethod]
    public async Task ScheduleSearch_NoContainer_ShowsError()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleSearchAsync();

        view.Mock.Verify(v => v.ShowError("Select a container first."), Times.Once);
    }

    [TestMethod]
    public async Task ScheduleAdd_LoadsLookupsAndSwitches()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);
        var container = ModelBuilder.Container(8);

        view.ContainerBinding.DataSource = new List<ContainerModel> { container };
        view.ContainerBinding.Position = 0;

        availabilityService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AvailabilityGroupModel>());

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleAddAsync();

        view.Mock.Verify(v => v.SwitchToScheduleEditMode(), Times.Once);
        view.Mock.Object.ScheduleContainerId.Should().Be(container.Id);
        view.Mock.Object.ScheduleCancelTarget.Should().Be(ScheduleViewModel.List);
    }

    [TestMethod]
    public async Task ScheduleEdit_PopulatesFieldsAndSwitchesToEdit()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);
        var schedule = ModelBuilder.Schedule(12, 2, "Sched");

        view.ScheduleBinding.DataSource = new List<ScheduleModel> { schedule };
        view.ScheduleBinding.Position = 0;
        view.Mock.Object.ScheduleMode = ScheduleViewModel.List;

        availabilityService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AvailabilityGroupModel>());
        scheduleService.Setup(s => s.GetDetailedAsync(schedule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleEditAsync();

        view.Mock.Object.ScheduleId.Should().Be(schedule.Id);
        view.Mock.Object.ScheduleName.Should().Be(schedule.Name);
        view.Mock.Object.IsEdit.Should().BeTrue();
        view.Mock.Verify(v => v.SwitchToScheduleEditMode(), Times.Once);
    }

    [TestMethod]
    public async Task ScheduleOpenProfile_LoadsDetailsAndSwitchesMode()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);
        var schedule = ModelBuilder.Schedule(20, 2, "Sched");

        view.ScheduleBinding.DataSource = new List<ScheduleModel> { schedule };
        view.ScheduleBinding.Position = 0;

        scheduleService.Setup(s => s.GetDetailedAsync(schedule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedule);

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleOpenProfileAsync();

        view.Mock.Verify(v => v.SetScheduleProfile(schedule), Times.Once);
        view.Mock.Verify(v => v.SwitchToScheduleProfileMode(), Times.Once);
        view.Mock.Object.ScheduleCancelTarget.Should().Be(ScheduleViewModel.List);
    }

    [TestMethod]
    public async Task ScheduleSave_Invalid_SetsValidationErrors()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.ScheduleName = "";
        view.Mock.Object.ScheduleContainerId = 0;
        view.Mock.Object.ScheduleYear = 2024;
        view.Mock.Object.ScheduleMonth = 1;
        view.Mock.Object.SchedulePeoplePerShift = 1;
        view.Mock.Object.ScheduleMaxHoursPerEmp = 10;
        view.Mock.Object.ScheduleShift1 = "9:00-17:00";
        view.Mock.Object.ScheduleShift2 = "18:00-22:00";

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleSaveAsync();

        view.Mock.Verify(v => v.SetScheduleValidationErrors(It.Is<IReadOnlyDictionary<string, string>>(errors => errors.Count > 0)), Times.Once);
        scheduleService.Verify(s => s.SaveWithDetailsAsync(
            It.IsAny<ScheduleModel>(),
            It.IsAny<IEnumerable<ScheduleEmployeeModel>>(),
            It.IsAny<IEnumerable<ScheduleSlotModel>>(),
            It.IsAny<IEnumerable<ScheduleCellStyleModel>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        view.Mock.Object.IsSuccessful.Should().BeFalse();
    }

    [TestMethod]
    public async Task ScheduleDelete_ConfirmTrue_DeletesAndReloads()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);
        var schedule = ModelBuilder.Schedule(30, 3, "Sched");

        view.ScheduleBinding.DataSource = new List<ScheduleModel> { schedule };
        view.ScheduleBinding.Position = 0;
        view.Mock.Setup(v => v.Confirm(It.IsAny<string>(), It.IsAny<string?>())).Returns(true);

        scheduleService.Setup(s => s.DeleteAsync(schedule.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        scheduleService.Setup(s => s.GetByContainerAsync(schedule.ContainerId, null, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ScheduleModel>());

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleDeleteAsync();

        scheduleService.Verify(s => s.DeleteAsync(schedule.Id, It.IsAny<CancellationToken>()), Times.Once);
        view.Mock.Verify(v => v.ShowInfo("Schedule deleted successfully."), Times.Once);
        view.Mock.Verify(v => v.SwitchToScheduleListMode(), Times.Once);
    }

    [TestMethod]
    public async Task ScheduleSave_Valid_CallsSaveAndReloads()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.ScheduleId = 0;
        view.Mock.Object.ScheduleContainerId = 5;
        view.Mock.Object.ScheduleName = "Sched";
        view.Mock.Object.ScheduleYear = 2024;
        view.Mock.Object.ScheduleMonth = 1;
        view.Mock.Object.SchedulePeoplePerShift = 1;
        view.Mock.Object.ScheduleShift1 = "9:00-17:00";
        view.Mock.Object.ScheduleShift2 = "18:00-22:00";
        view.Mock.Object.ScheduleMaxHoursPerEmp = 100;
        view.Mock.Object.ScheduleMaxConsecutiveDays = 5;
        view.Mock.Object.ScheduleMaxConsecutiveFull = 2;
        view.Mock.Object.ScheduleMaxFullPerMonth = 10;
        view.Mock.Object.ScheduleEmployees = new List<ScheduleEmployeeModel>
        {
            ModelBuilder.ScheduleEmployee(1, 10),
            ModelBuilder.ScheduleEmployee(1, 20)
        };
        view.Mock.Object.ScheduleSlots = new List<ScheduleSlotModel> { ModelBuilder.ScheduleSlot() };
        view.Mock.Object.ScheduleCancelTarget = ScheduleViewModel.List;

        scheduleService.Setup(s => s.SaveWithDetailsAsync(
                It.IsAny<ScheduleModel>(),
                It.IsAny<IEnumerable<ScheduleEmployeeModel>>(),
                It.IsAny<IEnumerable<ScheduleSlotModel>>(),
                It.IsAny<IEnumerable<ScheduleCellStyleModel>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        scheduleService.Setup(s => s.GetByContainerAsync(view.Mock.Object.ScheduleContainerId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleModel>());

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleSaveAsync();

        scheduleService.Verify(s => s.SaveWithDetailsAsync(
            It.Is<ScheduleModel>(m => m.Shift1Time == "09:00 - 17:00" && m.Shift2Time == "18:00 - 22:00"),
            It.Is<IEnumerable<ScheduleEmployeeModel>>(emps => emps.Count() == 1),
            It.IsAny<IEnumerable<ScheduleSlotModel>>(),
            It.IsAny<IEnumerable<ScheduleCellStyleModel>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        view.Mock.Verify(v => v.ShowInfo("Schedule added successfully."), Times.Once);
        view.Mock.Verify(v => v.SwitchToScheduleListMode(), Times.Once);
        view.Mock.Object.IsSuccessful.Should().BeTrue();
    }

    [TestMethod]
    public async Task ScheduleCancel_FromEdit_SwitchesToProfileTarget()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.ScheduleCancelTarget = ScheduleViewModel.Profile;

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleCancelAsync();

        view.Mock.Verify(v => v.ClearScheduleValidationErrors(), Times.Once);
        view.Mock.Verify(v => v.SwitchToScheduleProfileMode(), Times.Once);
    }

    [TestMethod]
    public async Task ScheduleGenerate_Invalid_ShowsErrors()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.ScheduleName = "";
        view.Mock.Object.ScheduleContainerId = 0;
        view.Mock.Object.ScheduleYear = 2024;
        view.Mock.Object.ScheduleMonth = 1;
        view.Mock.Object.SchedulePeoplePerShift = 1;
        view.Mock.Object.ScheduleMaxHoursPerEmp = 10;
        view.Mock.Object.ScheduleShift1 = "";
        view.Mock.Object.ScheduleShift2 = "";

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleGenerateAsync();

        view.Mock.Verify(v => v.SetScheduleValidationErrors(It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Once);
        view.Mock.Verify(v => v.ShowError("Please fix the highlighted fields."), Times.Once);
        generator.Verify(g => g.GenerateAsync(It.IsAny<ScheduleModel>(), It.IsAny<IEnumerable<AvailabilityGroupModel>>(), It.IsAny<IEnumerable<ScheduleEmployeeModel>>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ScheduleGenerate_NoGroupsSelected_ShowsError()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.ScheduleContainerId = 2;
        view.Mock.Object.ScheduleName = "Sched";
        view.Mock.Object.ScheduleYear = 2024;
        view.Mock.Object.ScheduleMonth = 1;
        view.Mock.Object.SchedulePeoplePerShift = 1;
        view.Mock.Object.ScheduleShift1 = "9:00-17:00";
        view.Mock.Object.ScheduleShift2 = "18:00-22:00";
        view.Mock.Object.ScheduleMaxHoursPerEmp = 100;

        view.Mock.Object.SelectedAvailabilityGroupId = 0;

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleGenerateAsync();

        view.Mock.Verify(v => v.ShowError("Select an availability group."), Times.Once);
    }

    [TestMethod]
    public async Task ScheduleGenerate_Valid_BuildsSlotsAndUpdatesView()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.ScheduleContainerId = 2;
        view.Mock.Object.ScheduleName = "Sched";
        view.Mock.Object.ScheduleYear = 2024;
        view.Mock.Object.ScheduleMonth = 1;
        view.Mock.Object.SchedulePeoplePerShift = 1;
        view.Mock.Object.ScheduleShift1 = "9:00-17:00";
        view.Mock.Object.ScheduleShift2 = "18:00-22:00";
        view.Mock.Object.ScheduleMaxHoursPerEmp = 100;
        view.Mock.Object.ScheduleMaxConsecutiveDays = 3;
        view.Mock.Object.ScheduleMaxConsecutiveFull = 2;
        view.Mock.Object.ScheduleMaxFullPerMonth = 10;

        view.Mock.Object.SelectedAvailabilityGroupId = 1;

        var group = ModelBuilder.AvailabilityGroup(1, "G1", 2024, 1);
        var member = ModelBuilder.AvailabilityMember(1, group.Id, 5);

        availabilityService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AvailabilityGroupModel> { group });
        availabilityService.Setup(s => s.LoadFullAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((group, new List<AvailabilityGroupMemberModel> { member }, new List<AvailabilityGroupDayModel>()));

        var slots = new List<ScheduleSlotModel> { ModelBuilder.ScheduleSlot(1, 1) };
        generator.Setup(g => g.GenerateAsync(It.IsAny<ScheduleModel>(), It.IsAny<IEnumerable<AvailabilityGroupModel>>(), It.IsAny<IEnumerable<ScheduleEmployeeModel>>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(slots);

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseScheduleGenerateAsync();

        view.Mock.Object.ScheduleEmployees.Should().NotBeNull();
        view.Mock.Object.ScheduleEmployees.Should().ContainSingle(e => e.EmployeeId == member.EmployeeId);
        view.Mock.Object.ScheduleSlots.Should().BeEquivalentTo(slots);
        view.Mock.Verify(v => v.ShowInfo("Slots generated. Review before saving."), Times.Once);
    }

    [TestMethod]
    public async Task Save_WhenServiceThrows_ShowsError()
    {
        var view = new ContainerViewHarness();
        var containerService = new Mock<IContainerService>(MockBehavior.Strict);
        var scheduleService = new Mock<IScheduleService>(MockBehavior.Strict);
        var availabilityService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var generator = new Mock<IScheduleGenerator>(MockBehavior.Strict);

        view.Mock.Object.ContainerName = "Main";
        view.Mock.Object.IsEdit = false;
        containerService.Setup(s => s.CreateAsync(It.IsAny<ContainerModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        _ = new ContainerPresenter(view.Mock.Object, containerService.Object, scheduleService.Object, availabilityService.Object, generator.Object);

        await view.RaiseSaveAsync();

        view.Mock.Verify(v => v.ShowError("boom"), Times.Once);
    }
}
