using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using FluentAssertions;
using Moq;
using WinFormsApp.Presenter.Employee;
using WinFormsApp.Presentation.Tests.Helpers;
using WinFormsApp.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WinFormsApp.Presentation.Tests;

[TestClass]
public class EmployeePresenterTests
{
    [TestMethod]
    public async Task InitializeAsync_LoadsEmployeesIntoBindingSource()
    {
        using var sync = new SynchronizationContextScope();
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var employees = new List<EmployeeModel> { ModelBuilder.Employee(1) };

        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        var presenter = new EmployeePresenter(view.Mock.Object, service.Object);

        await presenter.InitializeAsync();

        view.BindingSource.DataSource.Should().BeSameAs(employees);
        service.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Search_EmptyValue_UsesGetAllAsync()
    {
        using var sync = new SynchronizationContextScope();
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var employees = new List<EmployeeModel> { ModelBuilder.Employee(2) };

        view.Mock.Object.SearchValue = "";
        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        await view.RaiseSearchAsync();

        view.BindingSource.DataSource.Should().BeSameAs(employees);
        service.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(s => s.GetByValueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Search_WithValue_UsesGetByValueAsync()
    {
        using var sync = new SynchronizationContextScope();
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var employees = new List<EmployeeModel> { ModelBuilder.Employee(3) };

        view.Mock.Object.SearchValue = "alex";
        service.Setup(s => s.GetByValueAsync("alex", It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        await view.RaiseSearchAsync();

        view.BindingSource.DataSource.Should().BeSameAs(employees);
        service.Verify(s => s.GetByValueAsync("alex", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Add_ClearsInputsAndSwitchesToEditMode()
    {
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        await view.RaiseAddAsync();

        view.Mock.Object.IsEdit.Should().BeFalse();
        view.Mock.Object.IsSuccessful.Should().BeFalse();
        view.Mock.Object.Message.Should().Be("Fill the form and press Save.");
        view.Mock.Object.CancelTarget.Should().Be(EmployeeViewModel.List);
        view.Mock.Verify(v => v.ClearInputs(), Times.Once);
        view.Mock.Verify(v => v.ClearValidationErrors(), Times.Once);
        view.Mock.Verify(v => v.SwitchToEditMode(), Times.Once);
    }

    [TestMethod]
    public async Task Add_CallsClearValidationBeforeInputsAndSwitch()
    {
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var sequence = new MockSequence();

        view.Mock.InSequence(sequence).Setup(v => v.ClearValidationErrors());
        view.Mock.InSequence(sequence).Setup(v => v.ClearInputs());
        view.Mock.InSequence(sequence).Setup(v => v.SwitchToEditMode());

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        await view.RaiseAddAsync();

        view.Mock.Verify(v => v.ClearValidationErrors(), Times.Once);
        view.Mock.Verify(v => v.ClearInputs(), Times.Once);
        view.Mock.Verify(v => v.SwitchToEditMode(), Times.Once);
    }

    [TestMethod]
    public async Task Edit_UsesCurrentItemAndSetsEditMode()
    {
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var employee = ModelBuilder.Employee(7, "Sam", "Taylor");

        _ = new EmployeePresenter(view.Mock.Object, service.Object);
        view.BindingSource.DataSource = new List<EmployeeModel> { employee };
        view.BindingSource.Position = 0;
        view.Mock.Object.Mode = EmployeeViewModel.List;

        await view.RaiseEditAsync();

        view.Mock.Object.Id.Should().Be(employee.Id);
        view.Mock.Object.FirstName.Should().Be(employee.FirstName);
        view.Mock.Object.LastName.Should().Be(employee.LastName);
        view.Mock.Object.Email.Should().Be(employee.Email);
        view.Mock.Object.Phone.Should().Be(employee.Phone);
        view.Mock.Object.IsEdit.Should().BeTrue();
        view.Mock.Object.CancelTarget.Should().Be(EmployeeViewModel.List);
        view.Mock.Verify(v => v.SwitchToEditMode(), Times.Once);
    }

    [TestMethod]
    public async Task Cancel_FromEdit_SwitchesToProfileTarget()
    {
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);

        view.Mock.Object.Mode = EmployeeViewModel.Edit;
        view.Mock.Object.CancelTarget = EmployeeViewModel.Profile;

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        await view.RaiseCancelAsync();

        view.Mock.Verify(v => v.ClearValidationErrors(), Times.Once);
        view.Mock.Verify(v => v.SwitchToProfileMode(), Times.Once);
    }

    [TestMethod]
    public async Task OpenProfile_SetsProfileAndSwitchesMode()
    {
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var employee = ModelBuilder.Employee(8, "Taylor", "Case");

        _ = new EmployeePresenter(view.Mock.Object, service.Object);
        view.BindingSource.DataSource = new List<EmployeeModel> { employee };
        view.BindingSource.Position = 0;

        await view.RaiseOpenProfileAsync();

        view.Mock.Verify(v => v.SetProfile(employee), Times.Once);
        view.Mock.Verify(v => v.SwitchToProfileMode(), Times.Once);
        view.Mock.Object.CancelTarget.Should().Be(EmployeeViewModel.List);
    }

    [TestMethod]
    public async Task Save_InvalidModel_SetsValidationErrors()
    {
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);

        view.Mock.Object.FirstName = "";
        view.Mock.Object.LastName = "";

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        await view.RaiseSaveAsync();

        view.Mock.Verify(v => v.SetValidationErrors(It.Is<IReadOnlyDictionary<string, string>>(errors =>
            errors.ContainsKey("FirstName") && errors.ContainsKey("LastName"))), Times.Once);
        service.Verify(s => s.CreateAsync(It.IsAny<EmployeeModel>(), It.IsAny<CancellationToken>()), Times.Never);
        service.Verify(s => s.UpdateAsync(It.IsAny<EmployeeModel>(), It.IsAny<CancellationToken>()), Times.Never);
        view.Mock.Object.IsSuccessful.Should().BeFalse();
        view.Mock.Object.Message.Should().Be("Please fix the highlighted fields.");
    }

    [TestMethod]
    public async Task Save_ValidCreate_CallsCreateAndReloads()
    {
        using var sync = new SynchronizationContextScope();
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var employees = new List<EmployeeModel> { ModelBuilder.Employee(11) };

        view.Mock.Object.Mode = EmployeeViewModel.Edit;
        view.Mock.Object.CancelTarget = EmployeeViewModel.List;
        view.Mock.Object.FirstName = "Alex";
        view.Mock.Object.LastName = "Mason";
        view.Mock.Object.Email = "alex@example.com";
        view.Mock.Object.Phone = "123456";
        view.Mock.Object.IsEdit = false;

        service.Setup(s => s.CreateAsync(It.IsAny<EmployeeModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModelBuilder.Employee(11));
        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        await view.RaiseSaveAsync();

        service.Verify(s => s.CreateAsync(It.IsAny<EmployeeModel>(), It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        view.Mock.Verify(v => v.ShowInfo("Employee added successfully."), Times.Once);
        view.Mock.Verify(v => v.SwitchToListMode(), Times.Once);
        view.Mock.Object.IsSuccessful.Should().BeTrue();
        view.BindingSource.DataSource.Should().BeSameAs(employees);
    }

    [TestMethod]
    public async Task Save_ValidUpdate_CallsUpdateAndReloads()
    {
        using var sync = new SynchronizationContextScope();
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var employees = new List<EmployeeModel> { ModelBuilder.Employee(12) };

        view.Mock.Object.Mode = EmployeeViewModel.Edit;
        view.Mock.Object.CancelTarget = EmployeeViewModel.List;
        view.Mock.Object.Id = 12;
        view.Mock.Object.FirstName = "Dana";
        view.Mock.Object.LastName = "Jones";
        view.Mock.Object.IsEdit = true;

        service.Setup(s => s.UpdateAsync(It.IsAny<EmployeeModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        await view.RaiseSaveAsync();

        service.Verify(s => s.UpdateAsync(It.IsAny<EmployeeModel>(), It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        view.Mock.Verify(v => v.ShowInfo("Employee updated successfully."), Times.Once);
        view.Mock.Verify(v => v.SwitchToListMode(), Times.Once);
        view.Mock.Object.IsSuccessful.Should().BeTrue();
    }

    [TestMethod]
    public async Task Delete_ConfirmFalse_DoesNotDelete()
    {
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var employee = ModelBuilder.Employee(15);

        view.Mock.Setup(v => v.Confirm(It.IsAny<string>(), It.IsAny<string?>())).Returns(false);

        _ = new EmployeePresenter(view.Mock.Object, service.Object);
        view.BindingSource.DataSource = new List<EmployeeModel> { employee };
        view.BindingSource.Position = 0;

        await view.RaiseDeleteAsync();

        service.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        view.Mock.Verify(v => v.ShowInfo(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ConfirmTrue_DeletesAndReloads()
    {
        using var sync = new SynchronizationContextScope();
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var employee = ModelBuilder.Employee(16, "Pat", "Doe");
        var employees = new List<EmployeeModel> { ModelBuilder.Employee(17) };

        view.Mock.Setup(v => v.Confirm(It.IsAny<string>(), It.IsAny<string?>())).Returns(true);
        service.Setup(s => s.DeleteAsync(employee.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        _ = new EmployeePresenter(view.Mock.Object, service.Object);
        view.BindingSource.DataSource = new List<EmployeeModel> { employee };
        view.BindingSource.Position = 0;

        await view.RaiseDeleteAsync();

        service.Verify(s => s.DeleteAsync(employee.Id, It.IsAny<CancellationToken>()), Times.Once);
        view.Mock.Verify(v => v.ShowInfo("Employee deleted successfully."), Times.Once);
        view.Mock.Verify(v => v.SwitchToListMode(), Times.Once);
        view.Mock.Object.IsSuccessful.Should().BeTrue();
        view.BindingSource.DataSource.Should().BeSameAs(employees);
    }

    [TestMethod]
    public async Task Save_WhenServiceThrows_ShowsError()
    {
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);

        view.Mock.Object.FirstName = "Alex";
        view.Mock.Object.LastName = "Stone";
        view.Mock.Object.IsEdit = false;

        service.Setup(s => s.CreateAsync(It.IsAny<EmployeeModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        await view.RaiseSaveAsync();

        view.Mock.Verify(v => v.ShowError("boom"), Times.Once);
    }

    [TestMethod]
    public async Task Search_CancelledOperation_DoesNotApplyResults()
    {
        using var sync = new SynchronizationContextScope();
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var initial = new List<EmployeeModel> { ModelBuilder.Employee(20) };
        var tcs = new TaskCompletionSource<List<EmployeeModel>>(TaskCreationOptions.RunContinuationsAsynchronously);

        view.BindingSource.DataSource = initial;
        view.Mock.Object.SearchValue = "term";

        service.Setup(s => s.GetByValueAsync("term", It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        using var cts = new CancellationTokenSource();
        var task = view.RaiseSearchAsync(cts.Token);
        cts.Cancel();

        tcs.SetResult(new List<EmployeeModel> { ModelBuilder.Employee(21) });
        await task;

        view.BindingSource.DataSource.Should().BeSameAs(initial);
    }

    [TestMethod]
    public async Task Search_RapidRequests_DoNotApplyStaleResults()
    {
        using var sync = new SynchronizationContextScope();
        var view = new EmployeeViewHarness();
        var service = new Mock<IEmployeeService>(MockBehavior.Strict);
        var firstTcs = new TaskCompletionSource<List<EmployeeModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondList = new List<EmployeeModel> { ModelBuilder.Employee(31) };

        service.Setup(s => s.GetByValueAsync("first", It.IsAny<CancellationToken>()))
            .Returns(firstTcs.Task);
        service.Setup(s => s.GetByValueAsync("second", It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondList);

        _ = new EmployeePresenter(view.Mock.Object, service.Object);

        view.Mock.Object.SearchValue = "first";
        var firstTask = view.RaiseSearchAsync();

        view.Mock.Object.SearchValue = "second";
        await view.RaiseSearchAsync();

        firstTcs.SetResult(new List<EmployeeModel> { ModelBuilder.Employee(30) });
        await firstTask;

        view.BindingSource.DataSource.Should().BeSameAs(secondList);
    }
}
