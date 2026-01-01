using FluentAssertions;
using Moq;
using WinFormsApp.Presenter;
using WinFormsApp.Presentation.Tests.Helpers;
using WinFormsApp.ViewModel;

namespace WinFormsApp.Presentation.Tests;

[TestClass]
public class MainPresenterTests
{
    [TestMethod]
    public async Task Navigate_ShowsEmployeeViewAndSetsActivePage()
    {
        var view = new MainViewHarness();
        var factory = new Mock<IMdiViewFactory>(MockBehavior.Strict);
        var employeeForm = new FakeForm();

        factory.Setup(f => f.CreateEmployeeView()).Returns(employeeForm);

        _ = new MainPresenter(view.Mock.Object, factory.Object);

        await view.RaiseShowEmployeeAsync();

        view.ActivePage.Should().Be(NavPage.Employee);
        employeeForm.ShowCalls.Should().Be(1);
        employeeForm.BringToFrontCalls.Should().Be(1);
    }

    [TestMethod]
    public async Task Navigate_ReusesExistingView()
    {
        var view = new MainViewHarness();
        var factory = new Mock<IMdiViewFactory>(MockBehavior.Strict);
        var employeeForm = new FakeForm();

        factory.Setup(f => f.CreateEmployeeView()).Returns(employeeForm);

        _ = new MainPresenter(view.Mock.Object, factory.Object);

        await view.RaiseShowEmployeeAsync();
        await view.RaiseShowEmployeeAsync();

        factory.Verify(f => f.CreateEmployeeView(), Times.Once);
        employeeForm.ShowCalls.Should().Be(2);
    }

    [TestMethod]
    public async Task Navigate_DisposedView_CreatesNewInstance()
    {
        var view = new MainViewHarness();
        var factory = new Mock<IMdiViewFactory>(MockBehavior.Strict);
        var first = new FakeForm();
        var second = new FakeForm();

        factory.SetupSequence(f => f.CreateEmployeeView())
            .Returns(first)
            .Returns(second);

        _ = new MainPresenter(view.Mock.Object, factory.Object);

        await view.RaiseShowEmployeeAsync();
        first.Dispose();
        await view.RaiseShowEmployeeAsync();

        factory.Verify(f => f.CreateEmployeeView(), Times.Exactly(2));
        second.ShowCalls.Should().Be(1);
    }

    [TestMethod]
    public async Task Navigate_WhenCancelled_DoesNotShowView()
    {
        var view = new MainViewHarness();
        var factory = new Mock<IMdiViewFactory>(MockBehavior.Strict);

        _ = new MainPresenter(view.Mock.Object, factory.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await view.RaiseShowEmployeeAsync(cts.Token);

        factory.Verify(f => f.CreateEmployeeView(), Times.Never);
        view.ActivePage.Should().Be(NavPage.Employee);
    }

    [TestMethod]
    public async Task Navigate_ShowsAvailabilityView()
    {
        var view = new MainViewHarness();
        var factory = new Mock<IMdiViewFactory>(MockBehavior.Strict);
        var availabilityForm = new FakeForm();

        factory.Setup(f => f.CreateAvailabilityView()).Returns(availabilityForm);

        _ = new MainPresenter(view.Mock.Object, factory.Object);

        await view.RaiseShowAvailabilityAsync();

        view.ActivePage.Should().Be(NavPage.Availability);
        availabilityForm.ShowCalls.Should().Be(1);
    }

    [TestMethod]
    public async Task Navigate_ShowsContainerView()
    {
        var view = new MainViewHarness();
        var factory = new Mock<IMdiViewFactory>(MockBehavior.Strict);
        var containerForm = new FakeForm();

        factory.Setup(f => f.CreateContainerView()).Returns(containerForm);

        _ = new MainPresenter(view.Mock.Object, factory.Object);

        await view.RaiseShowContainerAsync();

        view.ActivePage.Should().Be(NavPage.Container);
        containerForm.ShowCalls.Should().Be(1);
    }
}
