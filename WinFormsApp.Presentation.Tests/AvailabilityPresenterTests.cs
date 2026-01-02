using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using FluentAssertions;
using Moq;
using WinFormsApp.Presenter.Availability;
using WinFormsApp.Presentation.Tests.Helpers;
using WinFormsApp.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WinFormsApp.Presentation.Tests;

[TestClass]
public class AvailabilityPresenterTests
{
    [TestMethod]
    public async Task InitializeAsync_LoadsGroupsEmployeesAndBinds()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        var groups = new List<AvailabilityGroupModel> { ModelBuilder.AvailabilityGroup(1) };
        var employees = new List<EmployeeModel> { ModelBuilder.Employee(1) };
        var binds = new List<BindModel> { ModelBuilder.Bind(1) };

        groupService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(groups);
        employeeService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(employees);
        bindService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(binds);

        var presenter = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await presenter.InitializeAsync();

        view.ListBindingSource.DataSource.Should().BeSameAs(groups);
        view.BindsBindingSource.DataSource.Should().BeSameAs(binds);
        view.Mock.Verify(v => v.SetEmployeeList(It.Is<IEnumerable<EmployeeModel>>(list => list.SequenceEqual(employees))), Times.Once);
    }

    [TestMethod]
    public async Task Search_EmptyValue_UsesGetAllAsync()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        var groups = new List<AvailabilityGroupModel> { ModelBuilder.AvailabilityGroup(2) };
        view.Mock.Object.SearchValue = "";

        groupService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(groups);

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseSearchAsync();

        view.ListBindingSource.DataSource.Should().BeSameAs(groups);
        groupService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        groupService.Verify(s => s.GetByValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Search_WithValue_UsesGetByValueAsync()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        var groups = new List<AvailabilityGroupModel> { ModelBuilder.AvailabilityGroup(3) };
        view.Mock.Object.SearchValue = "jan";

        groupService.Setup(s => s.GetByValueAsync("jan", It.IsAny<CancellationToken>())).ReturnsAsync(groups);

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseSearchAsync();

        view.ListBindingSource.DataSource.Should().BeSameAs(groups);
        groupService.Verify(s => s.GetByValueAsync("jan", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Add_ClearsInputsAndSwitchesToEditMode()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseAddAsync();

        view.Mock.Object.IsEdit.Should().BeFalse();
        view.Mock.Object.Message.Should().Be("Fill the form, add employees, set codes and press Save.");
        view.Mock.Object.CancelTarget.Should().Be(AvailabilityViewModel.List);
        view.Mock.Verify(v => v.ClearInputs(), Times.Once);
        view.Mock.Verify(v => v.ClearValidationErrors(), Times.Once);
        view.Mock.Verify(v => v.ResetGroupMatrix(), Times.Once);
        view.Mock.Verify(v => v.SwitchToEditMode(), Times.Once);
    }

    [TestMethod]
    public async Task AddEmployeeToGroup_WhenNoSelection_ShowsError()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        view.Mock.Object.EmployeeId = 0;

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseAddEmployeeAsync();

        view.Mock.Verify(v => v.ShowError("Select employee first."), Times.Once);
    }

    [TestMethod]
    public async Task RemoveEmployeeFromGroup_WhenNoSelection_ShowsError()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        view.Mock.Object.EmployeeId = 0;

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseRemoveEmployeeAsync();

        view.Mock.Verify(v => v.ShowError("Select employee first."), Times.Once);
    }

    [TestMethod]
    public async Task Edit_LoadsFullModelAndPopulatesMatrix()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        var group = ModelBuilder.AvailabilityGroup(10, "January", 2024, 1);
        var member = ModelBuilder.AvailabilityMember(20, group.Id, 5);
        var day1 = ModelBuilder.AvailabilityDay(member.Id, 1, AvailabilityKind.ANY);

        groupService.Setup(s => s.LoadFullAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((group, new List<AvailabilityGroupMemberModel> { member }, new List<AvailabilityGroupDayModel> { day1 }));

        view.ListBindingSource.DataSource = new List<AvailabilityGroupModel> { group };
        view.ListBindingSource.Position = 0;
        view.Mock.Object.Mode = AvailabilityViewModel.List;

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseEditAsync();

        view.Mock.Object.AvailabilityMonthId.Should().Be(group.Id);
        view.Mock.Object.AvailabilityMonthName.Should().Be(group.Name);
        view.Mock.Object.Year.Should().Be(group.Year);
        view.Mock.Object.Month.Should().Be(group.Month);
        view.Mock.Object.IsEdit.Should().BeTrue();
        view.Mock.Object.CancelTarget.Should().Be(AvailabilityViewModel.List);
        view.Mock.Verify(v => v.TryAddEmployeeColumn(member.EmployeeId, It.IsAny<string>()), Times.Once);
        view.Mock.Verify(v => v.SetEmployeeCodes(member.EmployeeId,
            It.Is<IList<(int dayOfMonth, string code)>>(codes =>
                codes.Count == DateTime.DaysInMonth(group.Year, group.Month) && codes[0].code == "+")), Times.Once);
    }

    [TestMethod]
    public async Task Save_InvalidGroup_SetsValidationErrors()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        view.Mock.Object.AvailabilityMonthName = "";
        view.Mock.Object.Year = 1000;
        view.Mock.Object.Month = 13;

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseSaveAsync();

        view.Mock.Verify(v => v.SetValidationErrors(It.Is<IReadOnlyDictionary<string, string>>(errors => errors.Count >= 2)), Times.Once);
        groupService.Verify(s => s.SaveGroupAsync(It.IsAny<AvailabilityGroupModel>(), It.IsAny<IList<(int employeeId, IList<AvailabilityGroupDayModel> days)>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Save_NoEmployeesSelected_ShowsError()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        view.Mock.Object.AvailabilityMonthName = "January";
        view.Mock.Object.Year = DateTime.Today.Year;
        view.Mock.Object.Month = 1;
        view.Mock.Setup(v => v.GetSelectedEmployeeIds()).Returns(Array.Empty<int>());

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseSaveAsync();

        view.Mock.Verify(v => v.ShowError("Add at least 1 employee to the group."), Times.Once);
        groupService.Verify(s => s.SaveGroupAsync(It.IsAny<AvailabilityGroupModel>(), It.IsAny<IList<(int employeeId, IList<AvailabilityGroupDayModel> days)>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Save_ValidGroup_CallsServiceAndSwitchesToList()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        view.Mock.Object.AvailabilityMonthName = "January";
        view.Mock.Object.Year = DateTime.Today.Year;
        view.Mock.Object.Month = 1;
        view.Mock.Object.CancelTarget = AvailabilityViewModel.List;

        view.Mock.Setup(v => v.GetSelectedEmployeeIds()).Returns(new List<int> { 5 });
        view.Mock.Setup(v => v.ReadGroupCodes()).Returns(new List<(int employeeId, IList<(int dayOfMonth, string code)> codes)>
        {
            (5, new List<(int dayOfMonth, string code)> { (1, "+") })
        });

        groupService.Setup(s => s.SaveGroupAsync(It.IsAny<AvailabilityGroupModel>(), It.IsAny<IList<(int employeeId, IList<AvailabilityGroupDayModel> days)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groupService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AvailabilityGroupModel> { ModelBuilder.AvailabilityGroup(4) });

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseSaveAsync();

        groupService.Verify(s => s.SaveGroupAsync(It.IsAny<AvailabilityGroupModel>(), It.IsAny<IList<(int employeeId, IList<AvailabilityGroupDayModel> days)>>(), It.IsAny<CancellationToken>()), Times.Once);
        view.Mock.Verify(v => v.ShowInfo("Availability Group added successfully."), Times.Once);
        view.Mock.Verify(v => v.SwitchToListMode(), Times.Once);
        view.Mock.Object.IsSuccessful.Should().BeTrue();
    }

    [TestMethod]
    public async Task Delete_ConfirmTrue_DeletesAndReloads()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        var group = ModelBuilder.AvailabilityGroup(6, "Group");
        view.ListBindingSource.DataSource = new List<AvailabilityGroupModel> { group };
        view.ListBindingSource.Position = 0;

        view.Mock.Setup(v => v.Confirm(It.IsAny<string>(), It.IsAny<string?>())).Returns(true);
        groupService.Setup(s => s.DeleteAsync(group.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        groupService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AvailabilityGroupModel>());

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseDeleteAsync();

        groupService.Verify(s => s.DeleteAsync(group.Id, It.IsAny<CancellationToken>()), Times.Once);
        view.Mock.Verify(v => v.ShowInfo("Availability Group deleted successfully."), Times.Once);
        view.Mock.Verify(v => v.SwitchToListMode(), Times.Once);
    }

    [TestMethod]
    public async Task OpenProfile_LoadsProfileAndSwitchesMode()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        var group = ModelBuilder.AvailabilityGroup(7, "Group");
        view.ListBindingSource.DataSource = new List<AvailabilityGroupModel> { group };
        view.ListBindingSource.Position = 0;

        groupService.Setup(s => s.LoadFullAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((group, new List<AvailabilityGroupMemberModel>(), new List<AvailabilityGroupDayModel>()));

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseOpenProfileAsync();

        view.Mock.Verify(v => v.SetProfile(group, It.IsAny<List<AvailabilityGroupMemberModel>>(), It.IsAny<List<AvailabilityGroupDayModel>>()), Times.Once);
        view.Mock.Object.CancelTarget.Should().Be(AvailabilityViewModel.List);
        view.Mock.Verify(v => v.SwitchToProfileMode(), Times.Once);
    }

    [TestMethod]
    public async Task Save_WhenServiceThrows_ShowsError()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        view.Mock.Object.AvailabilityMonthName = "January";
        view.Mock.Object.Year = DateTime.Today.Year;
        view.Mock.Object.Month = 1;
        view.Mock.Setup(v => v.GetSelectedEmployeeIds()).Returns(new List<int> { 5 });
        view.Mock.Setup(v => v.ReadGroupCodes()).Returns(new List<(int employeeId, IList<(int dayOfMonth, string code)> codes)>
        {
            (5, new List<(int dayOfMonth, string code)> { (1, "+") })
        });

        groupService.Setup(s => s.SaveGroupAsync(It.IsAny<AvailabilityGroupModel>(), It.IsAny<IList<(int employeeId, IList<AvailabilityGroupDayModel> days)>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseSaveAsync();

        view.Mock.Verify(v => v.ShowError("boom"), Times.Once);
    }

    [TestMethod]
    public async Task AddBind_AddsNewBindRow()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseAddBindAsync();

        view.BindsBindingSource.Count.Should().Be(1);
    }

    [TestMethod]
    public async Task UpsertBind_InvalidMissingValue_ShowsError()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        var bind = new BindModel { Key = "F1", Value = "" };

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseUpsertBindAsync(bind);

        view.Mock.Verify(v => v.ShowError("Bind must contain both Key and Value."), Times.Once);
        bindService.Verify(s => s.CreateAsync(It.IsAny<BindModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task UpsertBind_ValidCreate_UpdatesBinds()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);
        var bind = new BindModel { Id = 0, Key = "F1", Value = "+" };

        bindService.Setup(s => s.CreateAsync(bind, It.IsAny<CancellationToken>())).ReturnsAsync(bind);
        bindService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<BindModel> { bind });

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseUpsertBindAsync(bind);

        bindService.Verify(s => s.CreateAsync(bind, It.IsAny<CancellationToken>()), Times.Once);
        view.BindsBindingSource.DataSource.Should().BeAssignableTo<List<BindModel>>();
    }

    [TestMethod]
    public async Task DeleteBind_Existing_RemovesAndReloads()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);
        var bind = ModelBuilder.Bind(9);

        bindService.Setup(s => s.DeleteAsync(bind.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        bindService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<BindModel>());

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseDeleteBindAsync(bind);

        bindService.Verify(s => s.DeleteAsync(bind.Id, It.IsAny<CancellationToken>()), Times.Once);
        view.BindsBindingSource.DataSource.Should().BeAssignableTo<List<BindModel>>();
    }

    [TestMethod]
    public async Task Search_WhenCancelled_DoesNotShowError()
    {
        var view = new AvailabilityViewHarness();
        var groupService = new Mock<IAvailabilityGroupService>(MockBehavior.Strict);
        var employeeService = new Mock<IEmployeeService>(MockBehavior.Strict);
        var bindService = new Mock<IBindService>(MockBehavior.Strict);

        view.Mock.Object.SearchValue = "jan";
        groupService.Setup(s => s.GetByValueAsync("jan", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        _ = new AvailabilityPresenter(view.Mock.Object, groupService.Object, employeeService.Object, bindService.Object);

        await view.RaiseSearchAsync();

        view.Mock.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never);
    }
}
