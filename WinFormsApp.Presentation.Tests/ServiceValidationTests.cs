using BusinessLogicLayer.Services;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ComponentModel.DataAnnotations;

namespace WinFormsApp.Presentation.Tests;

[TestClass]
public class ServiceValidationTests
{
    [TestMethod]
    public async Task EmployeeService_CreateAsync_DuplicateName_Throws()
    {
        var repo = new Mock<IEmployeeRepository>(MockBehavior.Strict);
        repo.Setup(r => r.ExistsByNameAsync("Alex", "Smith", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmployeeService(repo.Object);
        var model = new EmployeeModel { FirstName = "Alex", LastName = "Smith" };

        await FluentActions.Invoking(() => service.CreateAsync(model, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*same first and last name*");
    }

    [TestMethod]
    public async Task EmployeeService_DeleteAsync_WithReferences_Throws()
    {
        var repo = new Mock<IEmployeeRepository>(MockBehavior.Strict);
        repo.Setup(r => r.HasAvailabilityReferencesAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.HasScheduleReferencesAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new EmployeeService(repo.Object);

        await FluentActions.Invoking(() => service.DeleteAsync(7, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*delete this employee*Availability*Schedule*");
    }

    [TestMethod]
    public async Task ShopService_CreateAsync_DuplicateName_Throws()
    {
        var repo = new Mock<IShopRepository>(MockBehavior.Strict);
        repo.Setup(r => r.ExistsByNameAsync("Central", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ShopService(repo.Object);
        var model = new ShopModel { Name = "Central", Address = "Main St" };

        await FluentActions.Invoking(() => service.CreateAsync(model, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*shop with the same name*");
    }

    [TestMethod]
    public async Task ShopService_DeleteAsync_WithReferences_Throws()
    {
        var repo = new Mock<IShopRepository>(MockBehavior.Strict);
        repo.Setup(r => r.HasScheduleReferencesAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ShopService(repo.Object);

        await FluentActions.Invoking(() => service.DeleteAsync(3, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*delete this shop*Schedule*");
    }

    [TestMethod]
    public async Task AvailabilityGroupService_SaveGroupAsync_DuplicateName_Throws()
    {
        var groupRepo = new Mock<IAvailabilityGroupRepository>(MockBehavior.Strict);
        var memberRepo = new Mock<IAvailabilityGroupMemberRepository>(MockBehavior.Strict);
        var dayRepo = new Mock<IAvailabilityGroupDayRepository>(MockBehavior.Strict);

        groupRepo.Setup(r => r.ExistsByNameAsync("Group A", 2025, 8, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new AvailabilityGroupService(groupRepo.Object, memberRepo.Object, dayRepo.Object);
        var model = new AvailabilityGroupModel { Name = "Group A", Year = 2025, Month = 8 };

        await FluentActions.Invoking(() => service.SaveGroupAsync(model, Array.Empty<(int, IList<AvailabilityGroupDayModel>)>(), CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*availability group*same name*");
    }
}
