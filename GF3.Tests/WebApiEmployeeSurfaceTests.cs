using System.Security.Claims;
using System.Text;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Contracts.Availability;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using BusinessLogicLayer.Services.Abstractions;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Controllers;
using WebApi.Contracts.Auth;
using WebApi.Contracts.EmployeeAvailability;
using WebApi.Contracts.EmployeeProfile;
using WebApi.Contracts.EmployeeSchedules;
using WebApi.Options;
using WebApi.Realtime;
using WebApi.Services;
using WebApi.ShiftSwaps;

namespace GF3.Tests;

public sealed class WebApiEmployeeSurfaceTests
{
    [Fact]
    public async Task JwtAuthenticationHandler_AuthenticatesHeaderAndQueryTokens()
    {
        var options = CreateJwtOptions();
        var tokenService = new JwtTokenService(Options.Create(options));
        var token = tokenService.CreateAccessToken(new AuthenticatedSessionDto
        {
            UserName = "worker",
            Role = AuthRoles.Employee,
            DisplayName = "Worker Bee",
            EmployeeId = 12,
        });
        var provider = CreateJwtAuthenticationProvider(options);

        var headerContext = new DefaultHttpContext { RequestServices = provider };
        headerContext.Request.Headers.Authorization = $"Bearer {token.AccessToken}";
        var headerResult = await provider
            .GetRequiredService<IAuthenticationService>()
            .AuthenticateAsync(headerContext, JwtAuthenticationDefaults.SchemeName);

        var queryContext = new DefaultHttpContext { RequestServices = provider };
        queryContext.Request.QueryString = QueryString.Create("access_token", token.AccessToken);
        var queryResult = await provider
            .GetRequiredService<IAuthenticationService>()
            .AuthenticateAsync(queryContext, JwtAuthenticationDefaults.SchemeName);

        Assert.True(headerResult.Succeeded);
        Assert.Equal("worker", headerResult.Principal!.Identity!.Name);
        Assert.Equal(AuthRoles.Employee, headerResult.Principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal("Worker Bee", headerResult.Principal.FindFirstValue("display_name"));
        Assert.Equal("12", headerResult.Principal.FindFirstValue("employee_id"));
        Assert.True(queryResult.Succeeded);
    }

    [Fact]
    public async Task JwtAuthenticationHandler_RejectsMalformedAndTamperedTokens()
    {
        var options = CreateJwtOptions();
        var provider = CreateJwtAuthenticationProvider(options);
        var auth = provider.GetRequiredService<IAuthenticationService>();

        var malformedContext = new DefaultHttpContext { RequestServices = provider };
        malformedContext.Request.Headers.Authorization = "Bearer not-a-token";
        var malformedResult = await auth.AuthenticateAsync(malformedContext, JwtAuthenticationDefaults.SchemeName);

        var validToken = new JwtTokenService(Options.Create(options)).CreateAccessToken(new AuthenticatedSessionDto
        {
            UserName = "manager",
            Role = AuthRoles.Manager,
            DisplayName = "Manager",
            ManagerId = 3,
        }).AccessToken;
        var tamperedToken = validToken[..^1] + (validToken[^1] == 'a' ? "b" : "a");
        var tamperedContext = new DefaultHttpContext { RequestServices = provider };
        tamperedContext.Request.Headers.Authorization = $"Bearer {tamperedToken}";
        var tamperedResult = await auth.AuthenticateAsync(tamperedContext, JwtAuthenticationDefaults.SchemeName);

        Assert.False(malformedResult.Succeeded);
        Assert.False(tamperedResult.Succeeded);
    }

    [Fact]
    public void JwtAuthOptions_FromConfiguration_NormalizesFallbacks()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "  ",
                ["Jwt:Audience"] = " Frontend ",
                ["Jwt:SigningKey"] = " custom-key ",
                ["Jwt:AccessTokenMinutes"] = "0",
            })
            .Build();

        var options = JwtAuthOptions.FromConfiguration(configuration);

        Assert.Equal("GF3.WebApi", options.Issuer);
        Assert.Equal("Frontend", options.Audience);
        Assert.Equal("custom-key", options.SigningKey);
        Assert.Equal(720, options.AccessTokenMinutes);
    }

    [Fact]
    public void JwtAuthOptions_FromConfiguration_RejectsUnsafeProductionSigningKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "CHANGE_ME",
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JwtAuthOptions.FromConfiguration(configuration, requireExplicitSigningKey: true));

        Assert.Contains("Jwt:SigningKey", exception.Message);
    }

    [Fact]
    public void GraphManualColumnLabelResolver_ReadsFallbackCompactLegacyAndBase64Metadata()
    {
        var compact = "Visible note\n\n<!--GF3_GRAPH_META:{\"m\":[[2,\"Morning\",{\"1\":\"08:00\"}]]}-->";
        var legacyJson = Uri.EscapeDataString("{\"manualColumns\":[{\"id\":7,\"label\":\" Evening \"}]}");
        var legacy = $"[[GF3_GRAPH_META:{legacyJson}]]";
        var packedJson = "{\"m\":[[4,\"Packed label\"]]}";
        var packed = $"[[GF3_GRAPH_META:b64:{Base64UrlEncode(packedJson)}]]";

        Assert.Equal("Custom column", GraphManualColumnLabelResolver.Resolve("note", null));
        Assert.Equal("Custom 3", GraphManualColumnLabelResolver.Resolve("note", 3));
        Assert.Equal("Morning", GraphManualColumnLabelResolver.Resolve(compact, 2));
        Assert.Equal("Evening", GraphManualColumnLabelResolver.Resolve(legacy, 7));
        Assert.Equal("Packed label", GraphManualColumnLabelResolver.Resolve(packed, 4));
        Assert.Equal("Custom 9", GraphManualColumnLabelResolver.Resolve("[[GF3_GRAPH_META:not-json]]", 9));
    }

    [Fact]
    public void GraphManualColumnLabelResolver_FallsBackForEmptyWhitespaceAndMalformedColumnMetadata()
    {
        var emptyPayload = "[[GF3_GRAPH_META:]]";
        var emptyObject = "[[GF3_GRAPH_META:{}]]";
        var whitespaceLabel = "[[GF3_GRAPH_META:{\"m\":[[5,\"   \"]]}]]";
        var malformedCompact = "[[GF3_GRAPH_META:{\"m\":[{\"bad\":true},[\"oops\",\"Wrong\"],[6,\"Good\"]]}]]";
        var malformedLegacy = "[[GF3_GRAPH_META:{\"manualColumns\":[[1,\"bad\"],{\"id\":\"nope\",\"label\":\"Wrong\"},{\"id\":8,\"label\":\"Legacy Good\"}]}]]";

        Assert.Equal("Custom column", GraphManualColumnLabelResolver.Resolve("note", 0));
        Assert.Equal("Custom 5", GraphManualColumnLabelResolver.Resolve(null, 5));
        Assert.Equal("Custom 5", GraphManualColumnLabelResolver.Resolve("   ", 5));
        Assert.Equal("Custom 5", GraphManualColumnLabelResolver.Resolve(emptyPayload, 5));
        Assert.Equal("Custom 5", GraphManualColumnLabelResolver.Resolve(emptyObject, 5));
        Assert.Equal("Custom 5", GraphManualColumnLabelResolver.Resolve(whitespaceLabel, 5));
        Assert.Equal("Good", GraphManualColumnLabelResolver.Resolve(malformedCompact, 6));
        Assert.Equal("Legacy Good", GraphManualColumnLabelResolver.Resolve(malformedLegacy, 8));
    }

    [Fact]
    public async Task AuthController_LoginSessionAndPasswordReset_MapServiceResults()
    {
        var expiresAtUtc = DateTimeOffset.UtcNow.AddHours(1);
        var authService = new RecordingAuthService
        {
            Session = new AuthenticatedSessionDto
            {
                UserName = "worker",
                Role = AuthRoles.Employee,
                DisplayName = "Worker Bee",
                EmployeeId = 12,
            },
            DispatchResult = new PasswordResetDispatchResult
            {
                DeliveryHint = "w***@example.com",
                ExpiresAtUtc = expiresAtUtc,
            },
        };
        var controller = new AuthController(
            authService,
            new StubJwtTokenService("signed-token", expiresAtUtc),
            new NoopWorkflowLogService());

        var loginResult = await controller.Login(new LoginRequest
        {
            Username = "worker",
            Password = "password",
        }, CancellationToken.None);
        SetUser(controller, CreatePrincipal(
            new Claim(ClaimTypes.Name, "worker"),
            new Claim(ClaimTypes.Role, AuthRoles.Employee),
            new Claim("display_name", "Worker Bee"),
            new Claim("employee_id", "12")));
        var sessionResult = controller.GetSession();
        var dispatchResult = await controller.SendPasswordResetCode(new SendPasswordResetCodeRequest
        {
            Username = "worker",
        }, CancellationToken.None);
        var confirmResult = await controller.ConfirmPasswordReset(new CompleteForgotPasswordResetRequest
        {
            Username = "worker",
            Code = "123456",
            NewPassword = "new-password",
        }, CancellationToken.None);

        var loginDto = Assert.IsType<LoginResponseDto>(Assert.IsType<OkObjectResult>(loginResult.Result).Value);
        Assert.Equal("signed-token", loginDto.AccessToken);
        Assert.Equal(12, loginDto.Session.EmployeeId);
        Assert.Equal(("worker", "password"), authService.LastAuthentication);

        var sessionDto = Assert.IsType<SessionDto>(Assert.IsType<OkObjectResult>(sessionResult.Result).Value);
        Assert.Equal("worker", sessionDto.UserName);
        Assert.Equal(AuthRoles.Employee, sessionDto.Role);
        Assert.Equal(12, sessionDto.EmployeeId);

        var dispatchDto = Assert.IsType<WebApi.Contracts.Auth.PasswordResetCodeDispatchDto>(
            Assert.IsType<OkObjectResult>(dispatchResult.Result).Value);
        Assert.Equal("w***@example.com", dispatchDto.DeliveryHint);
        Assert.IsType<NoContentResult>(confirmResult);
        Assert.Equal("worker", authService.LastPasswordResetUsername);
        Assert.Equal(("worker", "123456", "new-password"), authService.LastConfirmedReset);
    }

    [Fact]
    public async Task AuthController_LoginReturnsUnauthorized_WhenCredentialsFail()
    {
        var controller = new AuthController(
            new RecordingAuthService(),
            new StubJwtTokenService("unused", DateTimeOffset.UtcNow),
            new NoopWorkflowLogService());

        var result = await controller.Login(new LoginRequest
        {
            Username = "missing",
            Password = "wrong",
        }, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(unauthorized.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
    }

    [Fact]
    public async Task EmployeeProfileController_UsesCurrentEmployeeClaimForAllRoutes()
    {
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15);
        var service = new RecordingEmployeeProfileService
        {
            Profile = new BusinessLogicLayer.Contracts.Employees.EmployeeProfileDto
            {
                EmployeeId = 12,
                Username = "worker",
                DisplayName = "Worker Bee",
                RecoveryEmail = "old@example.com",
                Phone = "111",
            },
            DispatchResult = new PasswordResetDispatchResult
            {
                DeliveryHint = "w***@example.com",
                ExpiresAtUtc = expiresAtUtc,
            },
        };
        var controller = new EmployeeProfileController(service, new NoopWorkflowLogService());
        SetEmployeeUser(controller, 12);

        var currentResult = await controller.GetCurrent(CancellationToken.None);
        var updateResult = await controller.UpdateCurrent(new UpdateEmployeeProfileRequest
        {
            RecoveryEmail = "new@example.com",
            Phone = "222",
        }, CancellationToken.None);
        var sendCodeResult = await controller.SendPasswordResetCode(CancellationToken.None);
        var confirmResult = await controller.ConfirmPasswordReset(new CompletePasswordResetRequest
        {
            Code = "123456",
            NewPassword = "new-password",
        }, CancellationToken.None);

        var currentDto = Assert.IsType<WebApi.Contracts.EmployeeProfile.EmployeeProfileDto>(
            Assert.IsType<OkObjectResult>(currentResult.Result).Value);
        var updatedDto = Assert.IsType<WebApi.Contracts.EmployeeProfile.EmployeeProfileDto>(
            Assert.IsType<OkObjectResult>(updateResult.Result).Value);
        var dispatchDto = Assert.IsType<WebApi.Contracts.EmployeeProfile.PasswordResetCodeDispatchDto>(
            Assert.IsType<OkObjectResult>(sendCodeResult.Result).Value);

        Assert.Equal(12, currentDto.EmployeeId);
        Assert.Equal("Worker Bee", currentDto.DisplayName);
        Assert.Equal("new@example.com", updatedDto.RecoveryEmail);
        Assert.Equal((12, "new@example.com", "222"), service.LastUpdatedContact);
        Assert.Equal(12, service.LastPasswordResetEmployeeId);
        Assert.Equal("w***@example.com", dispatchDto.DeliveryHint);
        Assert.IsType<NoContentResult>(confirmResult);
        Assert.Equal((12, "123456", "new-password"), service.LastConfirmedReset);
    }

    [Fact]
    public async Task EmployeeSchedulesController_MapsPublishedGraphsAndRejectsMissingEmployeeClaim()
    {
        var service = new RecordingContainerService
        {
            PublishedGraphs =
            [
                new ScheduleModel
                {
                    Id = 9,
                    ContainerId = 2,
                    Container = new ContainerModel { Id = 2, Name = "Main Container" },
                    ShopId = 4,
                    Shop = new ShopModel { Id = 4, Name = "Central Shop" },
                    Name = "May Schedule",
                    Year = 2026,
                    Month = 5,
                    Note = null,
                    PublicationStatus = SchedulePublicationStatus.Public,
                    Employees =
                    [
                        new ScheduleEmployeeModel
                        {
                            Id = 20,
                            EmployeeId = 12,
                            DisplayOrder = 2,
                            MinHoursMonth = 80,
                            Employee = new EmployeeModel { Id = 12, FirstName = "Zoe", LastName = "Young" },
                        },
                        new ScheduleEmployeeModel
                        {
                            Id = 21,
                            EmployeeId = 7,
                            DisplayOrder = 1,
                            Employee = new EmployeeModel { Id = 7, FirstName = "Adam", LastName = "Blue" },
                        },
                    ],
                    Slots =
                    [
                        new ScheduleSlotModel { Id = 3, DayOfMonth = 2, SlotNo = 1, EmployeeId = null, FromTime = "08:00", ToTime = "12:00" },
                        new ScheduleSlotModel { Id = 1, DayOfMonth = 2, SlotNo = 2, EmployeeId = 12, FromTime = "12:00", ToTime = "16:00" },
                        new ScheduleSlotModel { Id = 2, DayOfMonth = 1, SlotNo = 1, EmployeeId = 7, FromTime = "08:00", ToTime = "12:00" },
                    ],
                }
            ],
        };
        service.GraphsByContainer[2] =
        [
            new ScheduleModel
            {
                Id = 10,
                ContainerId = 2,
                ShopId = 4,
                Shop = new ShopModel { Id = 4, Name = "Central Shop" },
                Name = "Second May Schedule",
                Year = 2026,
                Month = 5,
                PublicationStatus = SchedulePublicationStatus.Public,
            },
        ];
        service.DetailedGraphs[(2, 10)] = new ScheduleModel
        {
            Id = 10,
            ContainerId = 2,
            Container = new ContainerModel { Id = 2, Name = "Main Container" },
            ShopId = 4,
            Shop = new ShopModel { Id = 4, Name = "Central Shop" },
            Name = "Second May Schedule",
            Year = 2026,
            Month = 5,
            PublicationStatus = SchedulePublicationStatus.Public,
            Slots =
            [
                new ScheduleSlotModel
                {
                    Id = 4,
                    DayOfMonth = 3,
                    SlotNo = 1,
                    EmployeeId = 7,
                    FromTime = "10:00",
                    ToTime = "14:00",
                },
                new ScheduleSlotModel
                {
                    Id = 5,
                    DayOfMonth = 2,
                    SlotNo = 1,
                    EmployeeId = 12,
                    FromTime = "17:00",
                    ToTime = "21:00",
                },
            ],
        };

        var lastUpdatedAtUtc = new DateTimeOffset(2026, 6, 28, 12, 45, 0, TimeSpan.Zero);
        var controller = new EmployeeSchedulesController(service, new StubScheduleLastUpdateService(lastUpdatedAtUtc));
        SetEmployeeUser(controller, 12);

        var result = await controller.GetVisible(CancellationToken.None);
        var missingClaimController = new EmployeeSchedulesController(service);
        SetUser(missingClaimController, CreatePrincipal(new Claim(ClaimTypes.Name, "worker")));
        var missingClaimException = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            missingClaimController.GetVisible(CancellationToken.None));

        var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EmployeeScheduleDto>>(
            Assert.IsType<OkObjectResult>(result.Result).Value));
        Assert.Equal(12, service.LastPublishedEmployeeId);
        Assert.Equal("Main Container", dto.ContainerName);
        Assert.Equal("Central Shop", dto.ShopName);
        Assert.Equal("public", dto.PublicationStatus);
        Assert.Equal("", dto.Note);
        Assert.Equal(lastUpdatedAtUtc, dto.LastUpdatedAtUtc);
        Assert.Equal([7, 12], dto.Employees.Select(employee => employee.EmployeeId));
        Assert.Equal([2, 1], dto.Slots.Select(slot => slot.Id));
        Assert.Collection(
            dto.RelatedScheduleAssignments,
            assignment =>
            {
                Assert.Equal(7, assignment.EmployeeId);
                Assert.Equal(3, assignment.DayOfMonth);
                Assert.Equal(10, assignment.ScheduleId);
                Assert.Equal("Second May Schedule", assignment.ScheduleName);
            },
            assignment =>
            {
                Assert.Equal(12, assignment.EmployeeId);
                Assert.Equal(2, assignment.DayOfMonth);
                Assert.Equal(10, assignment.ScheduleId);
                Assert.Equal("Second May Schedule", assignment.ScheduleName);
            });
        Assert.Equal("The current employee session is invalid.", missingClaimException.Message);
    }

    [Fact]
    public async Task EmployeeAvailabilityController_MapsVisibleDetailAndUpdatePayloads()
    {
        var group = new AvailabilityGroupModel
        {
            Id = 5,
            Name = "May Availability",
            Year = 2026,
            Month = 5,
            VisibleFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            VisibleToUtc = DateTimeOffset.UtcNow.AddDays(3),
        };
        var member = new AvailabilityGroupMemberModel
        {
            Id = 8,
            AvailabilityGroupId = group.Id,
            EmployeeId = 12,
            EmployeeLastModifiedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
        };
        var model = new EmployeeAvailabilityModel
        {
            Group = group,
            Member = member,
            CanSubmit = true,
            Days =
            [
                new AvailabilityGroupDayModel
                {
                    Id = 13,
                    AvailabilityGroupMemberId = member.Id,
                    DayOfMonth = 3,
                    Kind = AvailabilityKind.INT,
                    IntervalStr = "08:00 - 12:00",
                },
            ],
        };
        var service = new RecordingAvailabilityGroupService
        {
            PublishedAvailability = [model],
            AvailabilityById = model,
        };
        var notifier = new RecordingRealtimeNotifier();
        var controller = new EmployeeAvailabilityController(
            service,
            new NoopWorkflowLogService(),
            realtimeNotifier: notifier);
        SetEmployeeUser(controller, 12);

        var visibleResult = await controller.GetVisible(CancellationToken.None);
        var byIdResult = await controller.GetById(group.Id, CancellationToken.None);
        var updateResult = await controller.UpdateSlots(group.Id, new UpdateEmployeeAvailabilityRequest
        {
            Slots =
            [
                new UpdateEmployeeAvailabilitySlotRequest
                {
                    DayOfMonth = 4,
                    Kind = AvailabilityKind.ANY,
                    IntervalStr = null,
                },
            ],
        }, CancellationToken.None);

        var visibleDto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EmployeeAvailabilityGroupDto>>(
            Assert.IsType<OkObjectResult>(visibleResult.Result).Value));
        var byIdDto = Assert.IsType<EmployeeAvailabilityGroupDto>(Assert.IsType<OkObjectResult>(byIdResult.Result).Value);
        var updatedDto = Assert.IsType<EmployeeAvailabilityGroupDto>(Assert.IsType<OkObjectResult>(updateResult.Result).Value);

        Assert.Equal(12, service.LastVisibleEmployeeId);
        Assert.Equal((12, group.Id), service.LastDetailRequest);
        Assert.Equal((12, group.Id), service.LastSavedRequest);
        Assert.Equal(4, Assert.Single(service.LastSavedDays).DayOfMonth);
        Assert.Equal("May Availability", visibleDto.Name);
        Assert.False(visibleDto.IsEditLocked);
        Assert.Null(visibleDto.EditLockedBy);
        Assert.True(byIdDto.CanSubmit);
        Assert.False(byIdDto.IsEditLocked);
        Assert.Equal(3, Assert.Single(byIdDto.Slots).DayOfMonth);
        Assert.Equal(4, Assert.Single(updatedDto.Slots).DayOfMonth);
        var managerDataChange = Assert.Single(notifier.ManagerDataChanges);
        Assert.Equal(ManagerEditResourceTypes.AvailabilityGroup, managerDataChange.ResourceType);
        Assert.Equal(group.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), managerDataChange.ResourceId);
        Assert.Equal("employee-availability-updated", managerDataChange.Reason);
    }

    [Fact]
    public async Task EmployeeAvailabilityController_BlocksUpdates_WhenManagerEditLockIsActive()
    {
        var group = new AvailabilityGroupModel
        {
            Id = 6,
            Name = "Locked Availability",
            Year = 2026,
            Month = 6,
            VisibleFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            VisibleToUtc = DateTimeOffset.UtcNow.AddDays(3),
        };
        var member = new AvailabilityGroupMemberModel
        {
            Id = 9,
            AvailabilityGroupId = group.Id,
            EmployeeId = 12,
        };
        var model = new EmployeeAvailabilityModel
        {
            Group = group,
            Member = member,
            CanSubmit = true,
        };
        var service = new RecordingAvailabilityGroupService
        {
            PublishedAvailability = [model],
            AvailabilityById = model,
        };
        var editLockService = new ManagerEditLockService();
        editLockService.SetLocks(
            "manager-connection",
            managerId: 3,
            "Chief",
            [ManagerEditLockTargets.AvailabilityGroup(group.Id)]);
        var controller = new EmployeeAvailabilityController(service, new NoopWorkflowLogService(), editLockService);
        SetEmployeeUser(controller, 12);

        var visibleResult = await controller.GetVisible(CancellationToken.None);
        var updateResult = await controller.UpdateSlots(group.Id, new UpdateEmployeeAvailabilityRequest
        {
            Slots =
            [
                new UpdateEmployeeAvailabilitySlotRequest
                {
                    DayOfMonth = 4,
                    Kind = AvailabilityKind.ANY,
                },
            ],
        }, CancellationToken.None);

        var visibleDto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<EmployeeAvailabilityGroupDto>>(
            Assert.IsType<OkObjectResult>(visibleResult.Result).Value));
        var conflict = Assert.IsType<ConflictObjectResult>(updateResult.Result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);

        Assert.False(visibleDto.CanSubmit);
        Assert.True(visibleDto.IsEditLocked);
        Assert.Equal("Chief", visibleDto.EditLockedBy);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("edit_lock_conflict", problem.Type);
        Assert.Equal("This availability group is currently being edited by Chief.", problem.Detail);
        Assert.Null(service.LastSavedRequest);
        Assert.Empty(service.LastSavedDays);
    }

    private static JwtAuthOptions CreateJwtOptions() => new()
    {
        Issuer = "GF3.Tests",
        Audience = "GF3.Tests.FrontEnd",
        SigningKey = "GF3-tests-signing-key-with-enough-entropy",
        AccessTokenMinutes = 30,
    };

    private static ServiceProvider CreateJwtAuthenticationProvider(JwtAuthOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(Options.Create(options));
        services
            .AddAuthentication(JwtAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>(
                JwtAuthenticationDefaults.SchemeName,
                _ => { });

        return services.BuildServiceProvider();
    }

    private static string Base64UrlEncode(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, JwtAuthenticationDefaults.SchemeName, ClaimTypes.Name, ClaimTypes.Role));

    private static void SetEmployeeUser(ControllerBase controller, int employeeId)
        => SetUser(controller, CreatePrincipal(
            new Claim(ClaimTypes.Name, "worker"),
            new Claim(ClaimTypes.Role, AuthRoles.Employee),
            new Claim("employee_id", employeeId.ToString(System.Globalization.CultureInfo.InvariantCulture))));

    private static void SetUser(ControllerBase controller, ClaimsPrincipal user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user,
            },
        };
    }

    private sealed class RecordingAuthService : IAuthService
    {
        public AuthenticatedSessionDto? Session { get; init; }
        public PasswordResetDispatchResult DispatchResult { get; init; } = new();
        public (string Username, string Password)? LastAuthentication { get; private set; }
        public string? LastPasswordResetUsername { get; private set; }
        public (string Username, string Code, string NewPassword)? LastConfirmedReset { get; private set; }

        public Task<AuthenticatedSessionDto?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
        {
            LastAuthentication = (username, password);
            return Task.FromResult(Session);
        }

        public Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default)
        {
            LastPasswordResetUsername = username;
            return Task.FromResult(DispatchResult);
        }

        public Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default)
        {
            LastConfirmedReset = (username, code, newPassword);
            return Task.CompletedTask;
        }
    }

    private sealed class StubJwtTokenService(string accessToken, DateTimeOffset expiresAtUtc) : IJwtTokenService
    {
        public JwtTokenResult CreateAccessToken(AuthenticatedSessionDto session)
            => new()
            {
                AccessToken = accessToken,
                ExpiresAtUtc = expiresAtUtc,
            };
    }

    private sealed class RecordingEmployeeProfileService : IEmployeeProfileService
    {
        public BusinessLogicLayer.Contracts.Employees.EmployeeProfileDto Profile { get; init; } = new();
        public PasswordResetDispatchResult DispatchResult { get; init; } = new();
        public (int EmployeeId, string? RecoveryEmail, string? Phone)? LastUpdatedContact { get; private set; }
        public int? LastPasswordResetEmployeeId { get; private set; }
        public (int EmployeeId, string Code, string NewPassword)? LastConfirmedReset { get; private set; }

        public Task<BusinessLogicLayer.Contracts.Employees.EmployeeProfileDto> GetAsync(int employeeId, CancellationToken ct = default)
            => Task.FromResult(Profile);

        public Task<BusinessLogicLayer.Contracts.Employees.EmployeeProfileDto> UpdateContactAsync(
            int employeeId,
            string? recoveryEmail,
            string? phone,
            CancellationToken ct = default)
        {
            LastUpdatedContact = (employeeId, recoveryEmail, phone);
            return Task.FromResult(new BusinessLogicLayer.Contracts.Employees.EmployeeProfileDto
            {
                EmployeeId = employeeId,
                Username = Profile.Username,
                DisplayName = Profile.DisplayName,
                RecoveryEmail = recoveryEmail,
                Phone = phone,
            });
        }

        public Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(int employeeId, CancellationToken ct = default)
        {
            LastPasswordResetEmployeeId = employeeId;
            return Task.FromResult(DispatchResult);
        }

        public Task ConfirmPasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)
        {
            LastConfirmedReset = (employeeId, code, newPassword);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingContainerService : IContainerService
    {
        public List<ScheduleModel> PublishedGraphs { get; init; } = [];
        public Dictionary<int, List<ScheduleModel>> GraphsByContainer { get; } = [];
        public Dictionary<(int ContainerId, int GraphId), ScheduleModel> DetailedGraphs { get; } = [];
        public int? LastPublishedEmployeeId { get; private set; }

        public Task<List<ScheduleModel>> GetPublishedGraphsForEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            LastPublishedEmployeeId = employeeId;
            return Task.FromResult(PublishedGraphs);
        }

        public Task<ContainerModel> CreateAsync(ContainerModel entity, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<ContainerModel>> GetAllAsync(CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ContainerModel?> GetAsync(int id, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(ContainerModel entity, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<ScheduleModel>?> GetGraphsAsync(int containerId, CancellationToken ct = default)
            => Task.FromResult<List<ScheduleModel>?>(
                GraphsByContainer.TryGetValue(containerId, out var graphs) ? graphs : []);

        public Task<ScheduleModel?> GetGraphByIdAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult<ScheduleModel?>(DetailedGraphs.GetValueOrDefault((containerId, graphId)));

        public Task<ScheduleModel> CreateGraphAsync(int containerId, ScheduleModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateGraphAsync(int containerId, int graphId, ScheduleModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<SchedulePresetModel>?> GetSchedulePresetsAsync(int containerId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<SchedulePresetModel> CreateSchedulePresetAsync(int containerId, SchedulePresetModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<GenerateGraphResult> GenerateGraphAsync(
            int containerId,
            int graphId,
            bool overwrite,
            bool dryRun,
            IProgress<int>? progress,
            CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<GenerateGraphResult> GenerateGraphPreviewAsync(
            int containerId,
            ScheduleModel model,
            IEnumerable<ScheduleEmployeeModel> employees,
            IProgress<int>? progress,
            CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<ScheduleSlotModel>?> GetGraphSlotsAsync(int containerId, int graphId, CancellationToken ct = default)
            => Task.FromResult<List<ScheduleSlotModel>?>(
                DetailedGraphs.GetValueOrDefault((containerId, graphId))?.Slots.ToList() ?? []);

        public Task ReplaceGraphSlotsAsync(int containerId, int graphId, IEnumerable<ScheduleSlotModel> slots, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ScheduleSlotModel> CreateGraphSlotAsync(int containerId, int graphId, ScheduleSlotModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateGraphSlotAsync(int containerId, int graphId, int slotId, ScheduleSlotModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task DeleteGraphSlotAsync(int containerId, int graphId, int slotId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<ScheduleEmployeeModel>?> GetGraphEmployeesAsync(int containerId, int graphId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ScheduleEmployeeModel> AddGraphEmployeeAsync(int containerId, int graphId, ScheduleEmployeeModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, ScheduleEmployeeModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task RemoveGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<ScheduleCellStyleModel>?> GetGraphCellStylesAsync(int containerId, int graphId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<ScheduleCellStyleModel> UpsertGraphCellStyleAsync(int containerId, int graphId, ScheduleCellStyleModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task DeleteGraphCellStyleAsync(int containerId, int graphId, int styleId, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingRealtimeNotifier : IRealtimeNotifier
    {
        public List<ManagerDataChange> ManagerDataChanges { get; } = [];

        public Task NotifyScheduleChangedAsync(int containerId, int graphId, string reason)
            => Task.CompletedTask;

        public Task NotifyManagerDataChangedAsync(
            string resourceType,
            string? resourceId,
            string reason,
            int? containerId = null,
            int? graphId = null)
        {
            ManagerDataChanges.Add(new ManagerDataChange(resourceType, resourceId, reason, containerId, graphId));
            return Task.CompletedTask;
        }

        public Task NotifyShiftSwapsChangedAsync(int? containerId, int? graphId, int? scheduleId, string reason, int? shiftSwapId = null)
            => Task.CompletedTask;

        public Task NotifyWorkflowLogCreatedAsync(DataAccessLayer.Models.WorkflowLogEntryModel entry)
            => Task.CompletedTask;

        public Task NotifyScheduleEditLockChangedAsync(ScheduleEditLockState state)
            => Task.CompletedTask;

        public Task NotifyManagerEditLockChangedAsync(ManagerEditLockState state)
            => Task.CompletedTask;

        public sealed record ManagerDataChange(
            string ResourceType,
            string? ResourceId,
            string Reason,
            int? ContainerId,
            int? GraphId);
    }

    private sealed class StubScheduleLastUpdateService(DateTimeOffset lastUpdatedAtUtc) : IScheduleLastUpdateService
    {
        public Task<IReadOnlyDictionary<int, DateTimeOffset>> GetLastUpdatesAsync(
            IEnumerable<int> scheduleIds,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<int, DateTimeOffset>>(
                scheduleIds.Distinct().ToDictionary(scheduleId => scheduleId, _ => lastUpdatedAtUtc));
    }

    private sealed class RecordingAvailabilityGroupService : IAvailabilityGroupService
    {
        public List<EmployeeAvailabilityModel> PublishedAvailability { get; init; } = [];
        public EmployeeAvailabilityModel AvailabilityById { get; init; } = new();
        public int? LastVisibleEmployeeId { get; private set; }
        public (int EmployeeId, int GroupId)? LastDetailRequest { get; private set; }
        public (int EmployeeId, int GroupId)? LastSavedRequest { get; private set; }
        public List<AvailabilityGroupDayModel> LastSavedDays { get; private set; } = [];

        public Task<List<EmployeeAvailabilityModel>> GetPublishedForEmployeeAsync(
            int employeeId,
            DateTimeOffset nowUtc,
            CancellationToken ct = default)
        {
            LastVisibleEmployeeId = employeeId;
            return Task.FromResult(PublishedAvailability);
        }

        public Task<EmployeeAvailabilityModel> GetPublishedForEmployeeByIdAsync(
            int employeeId,
            int groupId,
            DateTimeOffset nowUtc,
            CancellationToken ct = default)
        {
            LastDetailRequest = (employeeId, groupId);
            return Task.FromResult(AvailabilityById);
        }

        public Task<EmployeeAvailabilityModel> SaveEmployeeAvailabilityAsync(
            int employeeId,
            int groupId,
            IList<AvailabilityGroupDayModel> days,
            DateTimeOffset nowUtc,
            CancellationToken ct = default)
        {
            LastSavedRequest = (employeeId, groupId);
            LastSavedDays = days.ToList();
            return Task.FromResult(new EmployeeAvailabilityModel
            {
                Group = AvailabilityById.Group,
                Member = AvailabilityById.Member,
                Days = LastSavedDays,
                CanSubmit = AvailabilityById.CanSubmit,
            });
        }

        public Task<AvailabilityGroupModel> CreateAsync(AvailabilityGroupModel entity, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<AvailabilityGroupModel>> GetAllAsync(CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<AvailabilityGroupModel?> GetAsync(int id, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(AvailabilityGroupModel entity, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task SaveGroupAsync(
            AvailabilityGroupModel group,
            IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload,
            CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)>
            LoadFullAsync(int groupId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<AvailabilityGroupMemberModel>> GetMembersAsync(int groupId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<AvailabilityGroupMemberModel> CreateMemberAsync(int groupId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateMemberAsync(int groupId, int memberId, AvailabilityGroupMemberModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task DeleteMemberAsync(int groupId, int memberId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<List<AvailabilityGroupDayModel>> GetSlotsAsync(int groupId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<AvailabilityGroupDayModel> CreateSlotAsync(int groupId, AvailabilityGroupDayModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateSlotAsync(int groupId, int slotId, AvailabilityGroupDayModel model, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task DeleteSlotAsync(int groupId, int slotId, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
