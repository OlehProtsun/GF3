using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Models.DataBaseContext;

/// <summary>
/// Central EF Core model for the whole application.
/// The context does two jobs:
/// 1. exposes aggregate roots as <see cref="DbSet{TEntity}"/>,
/// 2. defines relational invariants that must stay true regardless of which API/service writes data.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider? timeProvider = null) : DbContext(options)
{
    public DbSet<ContainerModel> Containers => Set<ContainerModel>();
    public DbSet<EmployeeModel> Employees => Set<EmployeeModel>();
    public DbSet<EmploymentContractSettingsModel> EmploymentContractSettings => Set<EmploymentContractSettingsModel>();
    public DbSet<ContractShiftApprovalRequestModel> ContractShiftApprovalRequests => Set<ContractShiftApprovalRequestModel>();
    public DbSet<EmployeeAccountModel> EmployeeAccounts => Set<EmployeeAccountModel>();
    public DbSet<ManagerAccountModel> ManagerAccounts => Set<ManagerAccountModel>();
    public DbSet<ManagerNoteModel> ManagerNotes => Set<ManagerNoteModel>();
    public DbSet<ManagerNotepadStateModel> ManagerNotepadStates => Set<ManagerNotepadStateModel>();
    public DbSet<ManagerGraphFillColorBindModel> ManagerGraphFillColorBinds => Set<ManagerGraphFillColorBindModel>();
    public DbSet<ManagerGraphTextColorBindModel> ManagerGraphTextColorBinds => Set<ManagerGraphTextColorBindModel>();
    public DbSet<ManagerShiftCorrectionSettingModel> ManagerShiftCorrectionSettings => Set<ManagerShiftCorrectionSettingModel>();
    public DbSet<SystemNewsMessageModel> SystemNewsMessages => Set<SystemNewsMessageModel>();
    public DbSet<SystemNewsReadModel> SystemNewsReads => Set<SystemNewsReadModel>();
    public DbSet<CommunicationMessageModel> CommunicationMessages => Set<CommunicationMessageModel>();
    public DbSet<EmployeeCommunicationDismissalModel> EmployeeCommunicationDismissals => Set<EmployeeCommunicationDismissalModel>();
    public DbSet<EmployeeScheduleColumnPreferenceModel> EmployeeScheduleColumnPreferences => Set<EmployeeScheduleColumnPreferenceModel>();
    public DbSet<EmployeeNotificationReadModel> EmployeeNotificationReads => Set<EmployeeNotificationReadModel>();
    public DbSet<EmployeePushSubscriptionModel> EmployeePushSubscriptions => Set<EmployeePushSubscriptionModel>();
    public DbSet<EmployeePushDeliveryModel> EmployeePushDeliveries => Set<EmployeePushDeliveryModel>();
    public DbSet<EmployeePinnedSwapModel> EmployeePinnedSwaps => Set<EmployeePinnedSwapModel>();
    public DbSet<ShopModel> Shops => Set<ShopModel>();
    public DbSet<ScheduleModel> Schedules => Set<ScheduleModel>();
    public DbSet<SchedulePresetModel> SchedulePresets => Set<SchedulePresetModel>();
    public DbSet<SchedulePresetEmployeeModel> SchedulePresetEmployees => Set<SchedulePresetEmployeeModel>();
    public DbSet<ScheduleEmployeeModel> ScheduleEmployees => Set<ScheduleEmployeeModel>();
    public DbSet<ScheduleSlotModel> ScheduleSlots => Set<ScheduleSlotModel>();
    public DbSet<ScheduleCellStyleModel> ScheduleCellStyles => Set<ScheduleCellStyleModel>();
    public DbSet<ScheduleVersionModel> ScheduleVersions => Set<ScheduleVersionModel>();
    public DbSet<ScheduleVersionStateModel> ScheduleVersionStates => Set<ScheduleVersionStateModel>();
    public DbSet<ShiftSwapRequestModel> ShiftSwapRequests => Set<ShiftSwapRequestModel>();
    public DbSet<ShiftSwapHistoryModel> ShiftSwapHistories => Set<ShiftSwapHistoryModel>();
    public DbSet<ShiftCorrectionRequestModel> ShiftCorrectionRequests => Set<ShiftCorrectionRequestModel>();
    public DbSet<WorkflowLogEntryModel> WorkflowLogEntries => Set<WorkflowLogEntryModel>();
    public DbSet<WorkflowLogSettingsModel> WorkflowLogSettings => Set<WorkflowLogSettingsModel>();
    public DbSet<PublicHolidaySettingsModel> PublicHolidaySettings => Set<PublicHolidaySettingsModel>();
    public DbSet<PriorityGenerationSettingsModel> PriorityGenerationSettings => Set<PriorityGenerationSettingsModel>();
    public DbSet<RegulationDocumentModel> RegulationDocuments => Set<RegulationDocumentModel>();
    public DbSet<RegulationAcceptanceModel> RegulationAcceptances => Set<RegulationAcceptanceModel>();
    public DbSet<BindModel> AvailabilityBinds => Set<BindModel>();
    public DbSet<AvailabilityGroupModel> AvailabilityGroups => Set<AvailabilityGroupModel>();
    public DbSet<AvailabilityGroupMemberModel> AvailabilityGroupMembers => Set<AvailabilityGroupMemberModel>();
    public DbSet<AvailabilityGroupDayModel> AvailabilityGroupDays => Set<AvailabilityGroupDayModel>();
    public DbSet<AvailabilityGroupDayTransferModel> AvailabilityGroupDayTransfers => Set<AvailabilityGroupDayTransferModel>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateSchedulePublicationTimestamps();
        UpdateAvailabilityNotificationTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateSchedulePublicationTimestamps();
        UpdateAvailabilityNotificationTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateSchedulePublicationTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<ScheduleModel>())
        {
            var publication = entry.Property(schedule => schedule.PublicationStatus);
            var timestamp = entry.Property(schedule => schedule.PublishedAtUtc);
            if (entry.State == EntityState.Added)
            {
                timestamp.CurrentValue = publication.CurrentValue == SchedulePublicationStatus.Public
                    ? DateTimeOffset.UtcNow : null;
            }
            else if (entry.State == EntityState.Modified)
            {
                timestamp.CurrentValue = publication.CurrentValue == SchedulePublicationStatus.Public &&
                    publication.OriginalValue != SchedulePublicationStatus.Public
                    ? DateTimeOffset.UtcNow : timestamp.OriginalValue;
            }
        }
    }

    private void UpdateAvailabilityNotificationTimestamps()
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();
        foreach (var entry in ChangeTracker.Entries<AvailabilityGroupModel>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            var publication = entry.Property(x => x.PublicationStatus);
            var published = entry.Property(x => x.PublishedAtUtc);
            var newPublication = publication.CurrentValue == AvailabilityPublicationStatus.Public &&
                (entry.State == EntityState.Added || publication.OriginalValue != AvailabilityPublicationStatus.Public);
            published.CurrentValue = newPublication ? now : entry.State == EntityState.Added ? null : published.OriginalValue;
            var available = entry.Property(x => x.NotificationAvailableAtUtc);
            var rescheduled = entry.State == EntityState.Modified && available.OriginalValue > now &&
                entry.Property(x => x.VisibleFromUtc).IsModified;
            available.CurrentValue = newPublication || rescheduled
                ? entry.Entity.VisibleFromUtc is { } start && start > now ? start : now
                : entry.State == EntityState.Added ? null : available.OriginalValue;
            var legacyIdentity = entry.Property(x => x.NotificationLegacyVisibleFromUtc);
            legacyIdentity.CurrentValue = entry.State == EntityState.Added ? null : legacyIdentity.OriginalValue;
        }
        foreach (var entry in ChangeTracker.Entries<AvailabilityGroupMemberModel>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            var joined = entry.Property(x => x.JoinedAtUtc);
            joined.CurrentValue = entry.State == EntityState.Added || entry.Property(x => x.EmployeeId).IsModified
                ? now : joined.OriginalValue;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeePushSubscriptionModel>(entity =>
        {
            entity.ToTable("employee_push_subscription");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EmployeeId).HasColumnName("employee_id");
            entity.Property(x => x.Endpoint).HasColumnName("endpoint").HasMaxLength(2048).IsRequired();
            entity.Property(x => x.P256dh).HasColumnName("p256dh").HasMaxLength(512).IsRequired();
            entity.Property(x => x.Auth).HasColumnName("auth").HasMaxLength(256).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));
            entity.HasIndex(x => x.Endpoint).IsUnique();
            entity.HasIndex(x => new { x.EmployeeId, x.Id });
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<EmployeePushDeliveryModel>(entity =>
        {
            entity.ToTable("employee_push_delivery");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(x => x.NotificationId).HasColumnName("notification_id").HasMaxLength(256).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));
            entity.Property(x => x.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc").HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));
            entity.Property(x => x.AttemptCount).HasColumnName("attempt_count");
            entity.Property(x => x.LeaseUntilUtc).HasColumnName("lease_until_utc").HasConversion(x => x.HasValue ? x.Value.UtcTicks : (long?)null, x => x.HasValue ? new DateTimeOffset(x.Value, TimeSpan.Zero) : null);
            entity.Property(x => x.LeaseToken).HasColumnName("lease_token").HasMaxLength(36);
            entity.Property(x => x.FinalizedAtUtc).HasColumnName("finalized_at_utc").HasConversion(x => x.HasValue ? x.Value.UtcTicks : (long?)null, x => x.HasValue ? new DateTimeOffset(x.Value, TimeSpan.Zero) : null);
            entity.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(32);
            entity.HasIndex(x => new { x.SubscriptionId, x.NotificationId }).IsUnique();
            entity.HasIndex(x => new { x.NextAttemptAtUtc, x.LeaseUntilUtc });
            entity.HasIndex(x => x.FinalizedAtUtc);
            entity.HasOne(x => x.Subscription).WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Cascade);
        });
        ConfigureContainer(modelBuilder);
        ConfigureEmployee(modelBuilder);
        ConfigureEmployeeAccount(modelBuilder);
        ConfigureManagerAccount(modelBuilder);
        ConfigureManagerNote(modelBuilder);
        ConfigureManagerNotepadState(modelBuilder);
        ConfigureManagerGraphFillColorBind(modelBuilder);
        ConfigureManagerGraphTextColorBind(modelBuilder);
        ConfigureManagerShiftCorrectionSetting(modelBuilder);
        ConfigureSystemNews(modelBuilder);
        ConfigureCommunicationMessage(modelBuilder);
        ConfigureEmployeeCommunicationDismissal(modelBuilder);
        ConfigureEmployeeScheduleColumnPreference(modelBuilder);
        ConfigureEmployeeNotificationRead(modelBuilder);
        ConfigureEmployeePinnedSwap(modelBuilder);
        ConfigureShop(modelBuilder);
        ConfigureSchedule(modelBuilder);
        ConfigureSchedulePreset(modelBuilder);
        ConfigureSchedulePresetEmployee(modelBuilder);
        ConfigureScheduleEmployee(modelBuilder);
        ConfigureScheduleSlot(modelBuilder);
        ConfigureScheduleCellStyle(modelBuilder);
        ConfigureScheduleVersion(modelBuilder);
        ConfigureScheduleVersionState(modelBuilder);
        ConfigureShiftSwapRequest(modelBuilder);
        ConfigureShiftSwapHistory(modelBuilder);
        ConfigureShiftCorrectionRequest(modelBuilder);
        ConfigureWorkflowLogEntry(modelBuilder);
        ConfigureWorkflowLogSettings(modelBuilder);
        ConfigurePublicHolidaySettings(modelBuilder);
        ConfigurePriorityGenerationSettings(modelBuilder);
        ConfigureRegulationDocument(modelBuilder);
        ConfigureRegulationAcceptance(modelBuilder);
        ConfigureAvailabilityBind(modelBuilder);
        ConfigureAvailabilityGroup(modelBuilder);
        ConfigureAvailabilityGroupMember(modelBuilder);
        ConfigureAvailabilityGroupDay(modelBuilder);
        ConfigureAvailabilityGroupDayTransfer(modelBuilder);
    }

    private static void ConfigureContainer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContainerModel>(entity =>
        {
            entity.Property(property => property.Name).IsRequired();
            entity.HasIndex(property => property.Name).IsUnique();
        });
    }

    private static void ConfigureEmployee(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmploymentContractSettingsModel>(entity =>
        {
            entity.ToTable(table => table.HasCheckConstraint("ck_employment_contract_settings_singleton", "id = 1"));
            entity.Property(x => x.RestrictSwapsToEightHours).HasDefaultValue(true);
            entity.Property(x => x.RequirePickupApproval).HasDefaultValue(true);
            entity.Property(x => x.RequireBelowMinimumGiveApproval).HasDefaultValue(true);
            entity.Property(x => x.AutomaticWeekdayAvailability).HasDefaultValue(true);
            entity.HasData(new EmploymentContractSettingsModel());
        });
        modelBuilder.Entity<EmployeeModel>(entity =>
        {
            entity.Property(x => x.ContractType).IsRequired().HasMaxLength(16).HasDefaultValue(EmploymentContractTypes.Zlecenie);
            entity.ToTable(table => table.HasCheckConstraint("ck_employee_contract_type", "contract_type IN ('zlecenie','o_prace')"));
            entity.Property(property => property.FirstName).IsRequired();
            entity.Property(property => property.LastName).IsRequired();
            entity.Property(property => property.Email).IsRequired(false);
            entity.Property(property => property.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.HasIndex(property => new { property.FirstName, property.LastName })
                .IsUnique()
                .HasDatabaseName("ux_employee_full_name")
                .HasFilter("\"is_deleted\" = 0");

            entity.HasOne(property => property.Account)
                .WithOne(account => account.Employee)
                .HasForeignKey<EmployeeAccountModel>(account => account.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEmployeeAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeAccountModel>(entity =>
        {
            entity.Property(property => property.Language)
                .IsRequired()
                .HasMaxLength(2)
                .HasDefaultValue("pl");

            entity.Property(property => property.Username)
                .IsRequired()
                .HasMaxLength(100)
                .UseCollation("NOCASE");

            entity.Property(property => property.PasswordHash).IsRequired();
            entity.Property(property => property.PasswordUpdatedAtUtc).IsRequired();
            entity.Property(property => property.LastLoginAtUtc).IsRequired(false);
            entity.Property(property => property.LastSeenAtUtc).IsRequired(false);
            entity.Property(property => property.PasswordResetCodeHash).IsRequired(false);
            entity.Property(property => property.PasswordResetRequestedAtUtc).IsRequired(false);
            entity.Property(property => property.PasswordResetExpiresAtUtc).IsRequired(false);

            entity.HasIndex(property => property.EmployeeId)
                .IsUnique()
                .HasDatabaseName("ux_employee_account_employee");

            entity.HasIndex(property => property.Username)
                .IsUnique()
                .HasDatabaseName("ux_employee_account_username");
        });
    }

    private static void ConfigureManagerAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagerAccountModel>(entity =>
        {
            entity.Property(property => property.Language)
                .IsRequired()
                .HasMaxLength(2)
                .HasDefaultValue("pl");

            entity.Property(property => property.Username)
                .IsRequired()
                .HasMaxLength(100)
                .UseCollation("NOCASE");

            entity.Property(property => property.DisplayName)
                .IsRequired()
                .HasMaxLength(160);

            entity.Property(property => property.RecoveryEmail)
                .IsRequired(false)
                .HasMaxLength(254);

            entity.Property(property => property.PasswordHash).IsRequired();
            entity.Property(property => property.PasswordUpdatedAtUtc).IsRequired();
            entity.Property(property => property.LastLoginAtUtc).IsRequired(false);
            entity.Property(property => property.PasswordResetCodeHash).IsRequired(false);
            entity.Property(property => property.PasswordResetRequestedAtUtc).IsRequired(false);
            entity.Property(property => property.PasswordResetExpiresAtUtc).IsRequired(false);
            entity.Property(property => property.IsSystem).IsRequired();
            entity.Property(property => property.CreatedAtUtc).IsRequired();
            entity.Property(property => property.UpdatedAtUtc).IsRequired();

            entity.HasIndex(property => property.Username)
                .IsUnique()
                .HasDatabaseName("ux_manager_account_username");

            entity.HasIndex(property => property.IsSystem)
                .IsUnique()
                .HasDatabaseName("ux_manager_account_system")
                .HasFilter("\"is_system\" = 1");
        });
    }

    private static void ConfigureManagerNote(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagerNoteModel>(entity =>
        {
            entity.Property(note => note.Title).IsRequired().HasMaxLength(160);
            entity.Property(note => note.Content).IsRequired().HasMaxLength(20_000);
            entity.Property(note => note.Color).IsRequired().HasMaxLength(16);
            entity.Property(note => note.CreatedAtUtc).IsRequired();
            entity.Property(note => note.UpdatedAtUtc).IsRequired();

            entity.HasOne(note => note.ManagerAccount)
                .WithMany()
                .HasForeignKey(note => note.ManagerAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(note => new { note.ManagerAccountId, note.UpdatedAtUtc })
                .HasDatabaseName("ix_manager_note_manager_updated");
        });
    }

    private static void ConfigureManagerNotepadState(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagerNotepadStateModel>(entity =>
        {
            entity.Property(state => state.Height).IsRequired();
            entity.Property(state => state.UpdatedAtUtc).IsRequired();

            entity.HasOne(state => state.ManagerAccount)
                .WithOne()
                .HasForeignKey<ManagerNotepadStateModel>(state => state.ManagerAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureManagerGraphFillColorBind(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagerGraphFillColorBindModel>(entity =>
        {
            entity.Property(bind => bind.Key).IsRequired().HasMaxLength(64);
            entity.Property(bind => bind.FillColor).IsRequired().HasMaxLength(7);

            entity.HasOne(bind => bind.ManagerAccount)
                .WithMany()
                .HasForeignKey(bind => bind.ManagerAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(bind => new { bind.ManagerAccountId, bind.Key })
                .IsUnique()
                .HasDatabaseName("ux_manager_graph_fill_bind_key");
            entity.HasIndex(bind => new { bind.ManagerAccountId, bind.FillColor })
                .IsUnique()
                .HasDatabaseName("ux_manager_graph_fill_bind_color");
        });
    }

    private static void ConfigureManagerGraphTextColorBind(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagerGraphTextColorBindModel>(entity =>
        {
            entity.Property(bind => bind.Key).IsRequired().HasMaxLength(64);
            entity.Property(bind => bind.TextColor).IsRequired().HasMaxLength(7);

            entity.HasOne(bind => bind.ManagerAccount)
                .WithMany()
                .HasForeignKey(bind => bind.ManagerAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(bind => new { bind.ManagerAccountId, bind.Key })
                .IsUnique()
                .HasDatabaseName("ux_manager_graph_text_bind_key");
            entity.HasIndex(bind => new { bind.ManagerAccountId, bind.TextColor })
                .IsUnique()
                .HasDatabaseName("ux_manager_graph_text_bind_color");
        });
    }

    private static void ConfigureManagerShiftCorrectionSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagerShiftCorrectionSettingModel>(entity =>
        {
            entity.Property(setting => setting.HighlightColor).IsRequired().HasMaxLength(7);
            entity.HasOne(setting => setting.ManagerAccount)
                .WithOne()
                .HasForeignKey<ManagerShiftCorrectionSettingModel>(setting => setting.ManagerAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSystemNews(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemNewsMessageModel>(entity =>
        {
            entity.Property(message => message.Title).IsRequired().HasMaxLength(160);
            entity.Property(message => message.Body).IsRequired().HasMaxLength(10_000);
            entity.Property(message => message.Audience).IsRequired().HasMaxLength(16);
            entity.Property(message => message.ImageUrl).IsRequired(false).HasMaxLength(2_800_000);
            entity.Property(message => message.VideoUrl).IsRequired(false).HasMaxLength(500);
            entity.Property(message => message.CreatedAtUtc).IsRequired();
            entity.Property(message => message.UpdatedAtUtc).IsRequired();
            entity.HasIndex(message => message.CreatedAtUtc).HasDatabaseName("ix_system_news_created");
        });

        modelBuilder.Entity<SystemNewsReadModel>(entity =>
        {
            entity.Property(read => read.AccountRole).IsRequired().HasMaxLength(16);
            entity.Property(read => read.ReadAtUtc).IsRequired();
            entity.HasOne(read => read.Message)
                .WithMany(message => message.Reads)
                .HasForeignKey(read => read.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(read => new { read.MessageId, read.AccountRole, read.AccountId })
                .IsUnique()
                .HasDatabaseName("ux_system_news_read_subject");
            entity.HasIndex(read => new { read.AccountRole, read.AccountId, read.ReadAtUtc })
                .HasDatabaseName("ix_system_news_read_subject_time");
        });
    }

    private static void ConfigureCommunicationMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommunicationMessageModel>(entity =>
        {
            entity.Property(message => message.Title)
                .IsRequired()
                .HasMaxLength(160);

            entity.Property(message => message.Body)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(message => message.DeadlineAtUtc).IsRequired();
            entity.Property(message => message.CreatedAtUtc).IsRequired();
            entity.Property(message => message.CreatedByManagerId).IsRequired(false);
            entity.Property(message => message.CreatedByManagerName)
                .IsRequired()
                .HasMaxLength(160);

            entity.HasOne(message => message.CreatedByManager)
                .WithMany()
                .HasForeignKey(message => message.CreatedByManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(message => message.DeadlineAtUtc)
                .HasDatabaseName("ix_communication_message_deadline");

            entity.HasIndex(message => message.VisibleFromUtc)
                .HasDatabaseName("ix_communication_message_visible_from");

            entity.HasIndex(message => message.CreatedAtUtc)
                .HasDatabaseName("ix_communication_message_created");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_communication_message_title", "length(trim(title)) > 0");
                table.HasCheckConstraint("ck_communication_message_body", "length(trim(body)) > 0");
                table.HasCheckConstraint("ck_communication_message_deadline", "deadline_at_utc > created_at_utc");
            });
        });
    }

    private static void ConfigureEmployeeCommunicationDismissal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeCommunicationDismissalModel>(entity =>
        {
            entity.Property(dismissal => dismissal.DismissedAtUtc).IsRequired();

            entity.HasOne(dismissal => dismissal.CommunicationMessage)
                .WithMany(message => message.Dismissals)
                .HasForeignKey(dismissal => dismissal.CommunicationMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dismissal => dismissal.Employee)
                .WithMany()
                .HasForeignKey(dismissal => dismissal.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(dismissal => new { dismissal.CommunicationMessageId, dismissal.EmployeeId })
                .IsUnique()
                .HasDatabaseName("ux_employee_comm_dismissal_msg_emp");

            entity.HasIndex(dismissal => new { dismissal.EmployeeId, dismissal.DismissedAtUtc })
                .HasDatabaseName("ix_employee_comm_dismissal_emp_time");
        });
    }

    private static void ConfigureEmployeeScheduleColumnPreference(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeScheduleColumnPreferenceModel>(entity =>
        {
            entity.Property(preference => preference.ColumnOrderJson).IsRequired();
            entity.Property(preference => preference.UpdatedAtUtc).IsRequired();

            entity.HasOne(preference => preference.Employee)
                .WithMany()
                .HasForeignKey(preference => preference.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(preference => preference.Schedule)
                .WithMany()
                .HasForeignKey(preference => preference.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(preference => new { preference.EmployeeId, preference.ScheduleId })
                .IsUnique()
                .HasDatabaseName("ux_emp_schedule_column_pref_emp_schedule");
        });
    }

    private static void ConfigureEmployeeNotificationRead(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeNotificationReadModel>(entity =>
        {
            entity.Property(read => read.NotificationId)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(read => read.ReadAtUtc).IsRequired();

            entity.HasOne(read => read.Employee)
                .WithMany()
                .HasForeignKey(read => read.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(read => new { read.EmployeeId, read.NotificationId })
                .IsUnique()
                .HasDatabaseName("ux_emp_notification_read_emp_notification");

            entity.HasIndex(read => new { read.EmployeeId, read.ReadAtUtc })
                .HasDatabaseName("ix_emp_notification_read_emp_time");
        });
    }

    private static void ConfigureEmployeePinnedSwap(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeePinnedSwapModel>(entity =>
        {
            entity.Property(pin => pin.PinnedAtUtc).IsRequired();

            entity.HasOne(pin => pin.Employee)
                .WithMany()
                .HasForeignKey(pin => pin.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pin => pin.ShiftSwap)
                .WithMany()
                .HasForeignKey(pin => pin.ShiftSwapId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pin => new { pin.EmployeeId, pin.ShiftSwapId })
                .IsUnique()
                .HasDatabaseName("ux_employee_pinned_swap_employee_swap");

            entity.HasIndex(pin => new { pin.EmployeeId, pin.PinnedAtUtc })
                .HasDatabaseName("ix_employee_pinned_swap_employee_time");
        });
    }
    private static void ConfigureShop(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShopModel>(entity =>
        {
            entity.Property(property => property.Name).IsRequired();
            entity.Property(property => property.Address).IsRequired();
            entity.Property(property => property.Description).IsRequired(false);
            entity.HasIndex(property => property.Name)
                .IsUnique()
                .HasDatabaseName("ux_shop_name");
        });
    }

    private static void ConfigureSchedule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduleModel>(entity =>
        {
            entity.HasOne(schedule => schedule.Container)
                .WithMany(container => container.Schedules)
                .HasForeignKey(schedule => schedule.ContainerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(schedule => schedule.Shop)
                .WithMany(shop => shop.Schedules)
                .HasForeignKey(schedule => schedule.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(schedule => schedule.AvailabilityGroup)
                .WithMany()
                .HasForeignKey(schedule => schedule.AvailabilityGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(schedule => schedule.ContainerId).HasDatabaseName("ix_sched_container");
            entity.HasIndex(schedule => new { schedule.ShopId, schedule.Year, schedule.Month }).HasDatabaseName("ix_sched_shop_month");
            entity.HasIndex(schedule => new { schedule.ContainerId, schedule.ShopId }).HasDatabaseName("ix_sched_container_shop");
            entity.HasIndex(schedule => schedule.AvailabilityGroupId).HasDatabaseName("ix_sched_avail_group");
            entity.HasIndex(schedule => schedule.PublicationStatus).HasDatabaseName("ix_sched_publication_status");

            entity.Property(schedule => schedule.Note).IsRequired(false);
            entity.Property(schedule => schedule.PublicationStatus)
                .HasConversion<string>()
                .HasDefaultValue(SchedulePublicationStatus.Private);
            entity.Property(schedule => schedule.AllowSwap).HasDefaultValue(true);
            entity.Property(schedule => schedule.AcceptedSwapHighlightColor)
                .HasMaxLength(7)
                .HasDefaultValue("#BBF7D0");
            entity.Property(schedule => schedule.AcceptedManualShiftHighlightColor)
                .HasMaxLength(7)
                .HasDefaultValue("#BAE6FD");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_schedule_month", "month BETWEEN 1 AND 12");
                table.HasCheckConstraint("ck_schedule_people_per_shift", "people_per_shift >= 1");
                table.HasCheckConstraint("ck_schedule_max_hours_per_emp_month", "max_hours_per_emp_month >= 0");
                table.HasCheckConstraint("ck_schedule_max_consecutive_days", "max_consecutive_days >= 1");
                table.HasCheckConstraint("ck_schedule_max_consecutive_full", "max_consecutive_full >= 1");
                table.HasCheckConstraint("ck_schedule_max_full_per_month", "max_full_per_month >= 0");
                table.HasCheckConstraint("ck_schedule_shift1_format", "shift1_time LIKE '__:__ - __:__'");
                table.HasCheckConstraint("ck_schedule_shift2_format", "shift2_time LIKE '__:__ - __:__'");
            });

            // Time-order correctness is also enforced by database triggers because SQLite check
            // constraints are intentionally kept string-based and simple here.
        });
    }

    private static void ConfigureSchedulePreset(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchedulePresetModel>(entity =>
        {
            entity.HasOne<ContainerModel>()
                .WithMany()
                .HasForeignKey(preset => preset.ContainerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<ShopModel>()
                .WithMany()
                .HasForeignKey(preset => preset.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<AvailabilityGroupModel>()
                .WithMany()
                .HasForeignKey(preset => preset.AvailabilityGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(preset => new { preset.ContainerId, preset.Name })
                .IsUnique()
                .HasDatabaseName("ux_schedule_preset_container_name");

            entity.HasIndex(preset => preset.ContainerId).HasDatabaseName("ix_schedule_preset_container");
            entity.HasIndex(preset => preset.ShopId).HasDatabaseName("ix_schedule_preset_shop");
            entity.HasIndex(preset => preset.AvailabilityGroupId).HasDatabaseName("ix_schedule_preset_avail_group");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_schedule_preset_month", "month BETWEEN 1 AND 12");
                table.HasCheckConstraint("ck_schedule_preset_people_per_shift", "people_per_shift >= 1");
                table.HasCheckConstraint("ck_schedule_preset_max_hours_per_emp_month", "max_hours_per_emp_month >= 1");
                table.HasCheckConstraint("ck_schedule_preset_max_consecutive_days", "max_consecutive_days >= 1");
                table.HasCheckConstraint("ck_schedule_preset_max_consecutive_full", "max_consecutive_full >= 1");
                table.HasCheckConstraint("ck_schedule_preset_max_full_per_month", "max_full_per_month >= 1");
                table.HasCheckConstraint("ck_schedule_preset_shift1_format", "shift1_time LIKE '__:__ - __:__'");
                table.HasCheckConstraint("ck_schedule_preset_shift2_format", "shift2_time LIKE '__:__ - __:__'");
            });
        });
    }

    private static void ConfigureSchedulePresetEmployee(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchedulePresetEmployeeModel>(entity =>
        {
            entity.HasOne(presetEmployee => presetEmployee.SchedulePreset)
                .WithMany(preset => preset.Employees)
                .HasForeignKey(presetEmployee => presetEmployee.SchedulePresetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<EmployeeModel>()
                .WithMany()
                .HasForeignKey(presetEmployee => presetEmployee.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(presetEmployee => new { presetEmployee.SchedulePresetId, presetEmployee.EmployeeId })
                .IsUnique()
                .HasDatabaseName("ux_schedule_preset_employee");

            entity.HasIndex(presetEmployee => presetEmployee.EmployeeId)
                .HasDatabaseName("ix_schedule_preset_employee_employee");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_schedule_preset_employee_min_hours", "min_hours_month >= 0");
            });
        });
    }

    private static void ConfigureScheduleEmployee(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduleEmployeeModel>(entity =>
        {
            entity.Property(x => x.IsPriority).IsRequired().HasDefaultValue(false);
            entity.HasOne(scheduleEmployee => scheduleEmployee.Schedule)
                .WithMany(schedule => schedule.Employees)
                .HasForeignKey(scheduleEmployee => scheduleEmployee.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(scheduleEmployee => scheduleEmployee.Employee)
                .WithMany(employee => employee.ScheduleEmployees)
                .HasForeignKey(scheduleEmployee => scheduleEmployee.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(scheduleEmployee => scheduleEmployee.DisplayOrder)
                .HasDefaultValue(0);

            entity.HasIndex(scheduleEmployee => new { scheduleEmployee.ScheduleId, scheduleEmployee.EmployeeId })
                .IsUnique();
        });
    }

    private static void ConfigureScheduleSlot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduleSlotModel>(entity =>
        {
            entity.HasOne(slot => slot.Schedule)
                .WithMany(schedule => schedule.Slots)
                .HasForeignKey(slot => slot.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(slot => slot.Employee)
                .WithMany(employee => employee.ScheduleSlots)
                .HasForeignKey(slot => slot.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(slot => slot.Status)
                .HasConversion<string>()
                .HasDefaultValue(SlotStatus.UNFURNISHED);

            entity.Property(slot => slot.FromTime).IsRequired();
            entity.Property(slot => slot.ToTime).IsRequired();

            entity.HasIndex(slot => new { slot.ScheduleId, slot.DayOfMonth, slot.FromTime, slot.ToTime, slot.SlotNo })
                .IsUnique();

            entity.HasIndex(slot => new { slot.ScheduleId, slot.DayOfMonth, slot.FromTime, slot.ToTime, slot.EmployeeId })
                .IsUnique()
                .HasDatabaseName("ux_slot_unique_emp_per_time")
                .HasFilter("employee_id IS NOT NULL");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_schedule_slot_dom", "day_of_month BETWEEN 1 AND 31");
                table.HasCheckConstraint("ck_schedule_slot_slot_no", "slot_no >= 1");
                table.HasCheckConstraint(
                    "ck_schedule_slot_status_pair",
                    "((status='UNFURNISHED' AND employee_id IS NULL) OR (status='ASSIGNED' AND employee_id IS NOT NULL))");
                table.HasCheckConstraint("ck_schedule_slot_time_format", "from_time LIKE '__:__' AND to_time LIKE '__:__'");
                table.HasCheckConstraint("ck_schedule_slot_time_order", "from_time < to_time");
            });
        });
    }

    private static void ConfigureScheduleCellStyle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduleCellStyleModel>(entity =>
        {
            entity.HasOne(style => style.Schedule)
                .WithMany(schedule => schedule.CellStyles)
                .HasForeignKey(style => style.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(style => style.Employee)
                .WithMany()
                .HasForeignKey(style => style.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(style => new { style.ScheduleId, style.DayOfMonth, style.EmployeeId })
                .IsUnique()
                .HasDatabaseName("ux_sched_cell_style");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_schedule_cell_style_dom", "day_of_month BETWEEN 1 AND 31");
            });
        });
    }

    private static void ConfigureScheduleVersion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduleVersionModel>(entity =>
        {
            entity.Property(version => version.BranchName).IsRequired().HasMaxLength(80);
            entity.Property(version => version.CreatedByManagerName).IsRequired().HasMaxLength(160);
            entity.Property(version => version.CreatedAtUtc).IsRequired();
            entity.Property(version => version.SnapshotJson).IsRequired();

            entity.HasOne(version => version.Schedule)
                .WithMany(schedule => schedule.Versions)
                .HasForeignKey(version => version.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(version => version.ParentVersion)
                .WithMany(version => version.ChildVersions)
                .HasForeignKey(version => version.ParentVersionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_schedule_version_number", "version_number >= 1");
                table.HasCheckConstraint("ck_schedule_version_counts", "employee_count >= 0 AND slot_count >= 0 AND cell_style_count >= 0");
            });
        });
    }

    private static void ConfigureScheduleVersionState(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduleVersionStateModel>(entity =>
        {
            entity.HasOne(state => state.Schedule)
                .WithOne(schedule => schedule.VersionState)
                .HasForeignKey<ScheduleVersionStateModel>(state => state.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(state => state.CurrentVersion)
                .WithMany()
                .HasForeignKey(state => state.CurrentVersionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureShiftSwapRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContractShiftApprovalRequestModel>(entity =>
        {
            entity.HasOne<EmployeeModel>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ScheduleModel>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ManagerAccountModel>().WithMany().HasForeignKey(x => x.ReviewedByManagerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ShiftSwapRequestModel>().WithMany().HasForeignKey(x => x.ShiftSwapRequestId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<ShiftSwapRequestModel>().WithMany().HasForeignKey(x => x.ResultingShiftSwapRequestId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.ScheduleId, x.Status, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.EmployeeId, x.Status, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.ShiftSwapRequestId, x.Status });
            entity.HasIndex(x => x.ShiftSwapRequestId).IsUnique()
                .HasDatabaseName("ux_contract_approval_one_pending_claim_per_offer")
                .HasFilter("kind='claim' AND status='pending' AND shift_swap_request_id IS NOT NULL");
            entity.HasIndex(x => new { x.EmployeeId, x.ShiftSwapRequestId, x.FromTime, x.ToTime }).IsUnique()
                .HasFilter("kind='claim' AND status='pending'");
            entity.HasIndex(x => new { x.EmployeeId, x.ScheduleSlotId }).IsUnique()
                .HasFilter("kind='below_minimum_give' AND status='pending'");
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_contract_approval_kind", "kind IN ('claim','below_minimum_give')");
                table.HasCheckConstraint("ck_contract_approval_status", "status IN ('pending','approved','rejected','cancelled','expired')");
                table.HasCheckConstraint("ck_contract_approval_period", "from_time LIKE '__:__' AND to_time LIKE '__:__' AND from_time < to_time");
                table.HasCheckConstraint("ck_contract_approval_review", "status NOT IN ('approved','rejected') OR (reviewed_by_manager_id IS NOT NULL AND reviewed_at_utc IS NOT NULL)");
            });
        });
        modelBuilder.Entity<ShiftSwapRequestModel>(entity =>
        {
            entity.HasOne<ContractShiftApprovalRequestModel>().WithMany()
                .HasForeignKey(x => x.ApprovedEmploymentExceptionApprovalId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(request => request.RequestKind).HasConversion<string>().HasDefaultValue(ShiftSwapRequestKind.Give);
            entity.HasIndex(request => new { request.ReturnScheduleId, request.Status })
                .HasDatabaseName("ix_shift_swap_return_schedule_status");
            entity.HasIndex(request => new { request.ReturnScheduleSlotId, request.Status }).IsUnique()
                .HasDatabaseName("ux_shift_swap_open_return_slot")
                .HasFilter("status = 'Open' AND request_kind = 'Exchange' AND return_schedule_slot_id IS NOT NULL");
            entity.HasOne(request => request.Schedule)
                .WithMany()
                .HasForeignKey(request => request.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(request => request.ScheduleSlot)
                .WithMany()
                .HasForeignKey(request => request.ScheduleSlotId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(request => request.FromEmployee)
                .WithMany()
                .HasForeignKey(request => request.FromEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.TargetEmployee)
                .WithMany()
                .HasForeignKey(request => request.TargetEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.AcceptedByEmployee)
                .WithMany()
                .HasForeignKey(request => request.AcceptedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(request => request.Visibility)
                .HasConversion<string>()
                .HasDefaultValue(ShiftSwapVisibility.Public);

            entity.Property(request => request.Status)
                .HasConversion<string>()
                .HasDefaultValue(ShiftSwapStatus.Open);

            entity.Property(request => request.OfferedFromTime).IsRequired(false);
            entity.Property(request => request.OfferedToTime).IsRequired(false);
            entity.Property(request => request.AllowPartialAcceptance).IsRequired().HasDefaultValue(false);
            entity.Property(request => request.IsManagerCreated)
                .HasDefaultValue(false);
            entity.Property(request => request.ManualColumnId).IsRequired(false);
            entity.Property(request => request.CreatedAtUtc).IsRequired();
            entity.Property(request => request.AcceptedAtUtc).IsRequired(false);
            entity.Property(request => request.CancelledAtUtc).IsRequired(false);

            entity.HasIndex(request => new { request.ScheduleSlotId, request.Status })
                .IsUnique()
                .HasDatabaseName("ux_shift_swap_open_slot")
                .HasFilter("status = 'Open'");
        });
    }

    private static void ConfigureShiftSwapHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShiftSwapHistoryModel>(entity =>
        {
            entity.Property(history => history.RequestKind).HasConversion<string>().HasDefaultValue(ShiftSwapRequestKind.Give);
            entity.HasOne(history => history.Schedule)
                .WithMany()
                .HasForeignKey(history => history.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(history => history.ScheduleName).IsRequired().HasMaxLength(200);
            entity.Property(history => history.ContainerName).IsRequired().HasMaxLength(200);
            entity.Property(history => history.ShopName).IsRequired().HasMaxLength(200);
            entity.Property(history => history.FromTime).IsRequired().HasMaxLength(5);
            entity.Property(history => history.ToTime).IsRequired().HasMaxLength(5);
            entity.Property(history => history.FromEmployeeName).IsRequired().HasMaxLength(200);
            entity.Property(history => history.TargetEmployeeName).IsRequired(false).HasMaxLength(200);
            entity.Property(history => history.AcceptedByEmployeeName).IsRequired().HasMaxLength(200);
            entity.Property(history => history.ManualColumnName).IsRequired(false).HasMaxLength(200);
            entity.Property(history => history.BeforeSnapshotJson).IsRequired();
            entity.Property(history => history.AfterSnapshotJson).IsRequired();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_shift_swap_history_dom", "day_of_month BETWEEN 1 AND 31");
                table.HasCheckConstraint("ck_shift_swap_history_month", "month BETWEEN 1 AND 12");
            });
        });
    }

    private static void ConfigureShiftCorrectionRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShiftCorrectionRequestModel>(entity =>
        {
            entity.HasOne(request => request.Schedule)
                .WithMany(schedule => schedule.ShiftCorrectionRequests)
                .HasForeignKey(request => request.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(request => request.Employee)
                .WithMany()
                .HasForeignKey(request => request.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(request => request.ReviewedByManager)
                .WithMany()
                .HasForeignKey(request => request.ReviewedByManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(request => request.OriginalFromTime).IsRequired().HasMaxLength(5);
            entity.Property(request => request.OriginalToTime).IsRequired().HasMaxLength(5);
            entity.Property(request => request.RequestedFromTime).IsRequired().HasMaxLength(5);
            entity.Property(request => request.RequestedToTime).IsRequired().HasMaxLength(5);
            entity.Property(request => request.Status)
                .HasConversion<string>()
                .HasDefaultValue(ShiftCorrectionStatus.Pending);
            entity.Property(request => request.CreatedAtUtc).IsRequired();

            entity.HasIndex(request => new { request.ScheduleSlotId, request.Status })
                .IsUnique()
                .HasDatabaseName("ux_shift_correction_pending_slot")
                .HasFilter("status = 'Pending'");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_shift_correction_dom", "day_of_month BETWEEN 1 AND 31");
                table.HasCheckConstraint("ck_shift_correction_time_format", "original_from_time LIKE '__:__' AND original_to_time LIKE '__:__' AND requested_from_time LIKE '__:__' AND requested_to_time LIKE '__:__'");
                table.HasCheckConstraint("ck_shift_correction_requested_order", "requested_from_time < requested_to_time");
            });
        });
    }

    private static void ConfigureWorkflowLogEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowLogEntryModel>(entity =>
        {
            entity.Property(log => log.OccurredAtUtc).IsRequired();
            entity.Property(log => log.ActorRole).IsRequired().HasMaxLength(32);
            entity.Property(log => log.ActorName).IsRequired().HasMaxLength(160);
            entity.Property(log => log.Action).IsRequired().HasMaxLength(512);

            entity.HasIndex(log => log.OccurredAtUtc)
                .HasDatabaseName("ix_workflow_log_occurred_at");

            entity.HasIndex(log => new { log.ActorRole, log.OccurredAtUtc })
                .HasDatabaseName("ix_workflow_log_role_time");
        });
    }

    private static void ConfigurePublicHolidaySettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PublicHolidaySettingsModel>(entity =>
        {
            entity.Property(settings => settings.ShowPublicHolidays).IsRequired().HasDefaultValue(true);
            entity.ToTable(table => table.HasCheckConstraint("ck_public_holiday_settings_singleton", "id = 1"));
            entity.HasData(new PublicHolidaySettingsModel());
        });
    }

    private static void ConfigurePriorityGenerationSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriorityGenerationSettingsModel>(entity =>
        {
            entity.ToTable("priority_generation_settings", table =>
            {
                table.HasCheckConstraint("CK_priority_generation_settings_singleton", "id = 1");
                table.HasCheckConstraint("CK_priority_generation_settings_shift_hours", "shift_hours BETWEEN 1 AND 12");
            });
            entity.Property(x => x.ShiftHours).IsRequired().HasDefaultValue(8);
            entity.Property(x => x.WeekendsOff).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.RespectAvailability).IsRequired().HasDefaultValue(true);
            entity.HasData(new PriorityGenerationSettingsModel());
        });
    }

    private static void ConfigureWorkflowLogSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowLogSettingsModel>(entity =>
        {
            entity.Property(settings => settings.IsEnabled).IsRequired().HasDefaultValue(true);
            entity.Property(settings => settings.Audience)
                .IsRequired()
                .HasMaxLength(16)
                .HasDefaultValue(WorkflowLogSettingsModel.AllAudience);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_workflow_log_settings_singleton", "id = 1");
                table.HasCheckConstraint(
                    "ck_workflow_log_settings_audience",
                    "audience IN ('all', 'managers', 'employees')");
            });

            entity.HasData(new WorkflowLogSettingsModel());
        });
    }

    private static void ConfigureRegulationDocument(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegulationDocumentModel>(entity =>
        {
            entity.Property(document => document.Title).IsRequired().HasMaxLength(160);
            entity.Property(document => document.Version).IsRequired().HasMaxLength(80).UseCollation("NOCASE");
            entity.Property(document => document.Message).IsRequired().HasMaxLength(4000);
            entity.Property(document => document.PdfFileName).IsRequired().HasMaxLength(255);
            entity.Property(document => document.PdfContent).IsRequired();
            entity.Property(document => document.PdfSha256).IsRequired().HasMaxLength(64);
            entity.Property(document => document.CreatedByManagerName).IsRequired().HasMaxLength(160);
            entity.Property(document => document.IsPublished).IsRequired().HasDefaultValue(false);
            entity.Property(document => document.PublishedAtUtc).IsRequired(false);
            entity.Property(document => document.CreatedAtUtc).IsRequired();
            entity.Property(document => document.UpdatedAtUtc).IsRequired();
            entity.HasIndex(document => document.Version).IsUnique().HasDatabaseName("ux_regulation_document_version");
            entity.HasIndex(document => new { document.IsPublished, document.PublishedAtUtc })
                .HasDatabaseName("ix_regulation_document_published");
        });
    }

    private static void ConfigureRegulationAcceptance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegulationAcceptanceModel>(entity =>
        {
            entity.Property(acceptance => acceptance.AccountRole).IsRequired().HasMaxLength(16);
            entity.Property(acceptance => acceptance.UsernameSnapshot).IsRequired().HasMaxLength(100);
            entity.Property(acceptance => acceptance.DisplayNameSnapshot).IsRequired().HasMaxLength(160);
            entity.Property(acceptance => acceptance.AcceptedAtUtc).IsRequired();
            entity.HasOne(acceptance => acceptance.RegulationDocument)
                .WithMany(document => document.Acceptances)
                .HasForeignKey(acceptance => acceptance.RegulationDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(acceptance => new { acceptance.RegulationDocumentId, acceptance.AccountRole, acceptance.AccountId })
                .IsUnique()
                .HasDatabaseName("ux_regulation_acceptance_subject");
            entity.HasIndex(acceptance => new { acceptance.AccountRole, acceptance.AccountId, acceptance.AcceptedAtUtc })
                .HasDatabaseName("ix_regulation_acceptance_subject_time");
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_regulation_acceptance_role",
                "account_role IN ('manager', 'employee')"));
        });
    }

    private static void ConfigureAvailabilityBind(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BindModel>(entity =>
        {
            entity.Property(bind => bind.Key).IsRequired();
            entity.Property(bind => bind.Value).IsRequired();
            entity.HasOne(bind => bind.ManagerAccount)
                .WithMany()
                .HasForeignKey(bind => bind.ManagerAccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(bind => new { bind.ManagerAccountId, bind.Key })
                .IsUnique()
                .HasDatabaseName("ux_availability_bind_manager_key");
            entity.HasIndex(bind => bind.Key)
                .IsUnique()
                .HasDatabaseName("ux_availability_bind_global_key")
                .HasFilter("\"ManagerAccountId\" IS NULL");
        });
    }

    private static void ConfigureAvailabilityGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AvailabilityGroupModel>(entity =>
        {
            entity.Property(group => group.Name).IsRequired();
            entity.Property(group => group.PublicationStatus)
                .HasConversion<string>()
                .HasDefaultValue(AvailabilityPublicationStatus.Private);
            entity.Property(group => group.VisibleFromUtc).IsRequired(false);
            entity.Property(group => group.VisibleToUtc).IsRequired(false);

            entity.HasIndex(group => new { group.Year, group.Month, group.Name })
                .IsUnique()
                .HasDatabaseName("ux_avail_group_year_month_name");
            entity.HasIndex(group => new { group.PublicationStatus, group.VisibleFromUtc, group.VisibleToUtc })
                .HasDatabaseName("ix_avail_group_publication_visibility");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_availability_group_month", "month BETWEEN 1 AND 12");
            });
        });
    }

    private static void ConfigureAvailabilityGroupMember(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AvailabilityGroupMemberModel>(entity =>
        {
            entity.HasOne(member => member.AvailabilityGroup)
                .WithMany(group => group.Members)
                .HasForeignKey(member => member.AvailabilityGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(member => member.Employee)
                .WithMany()
                .HasForeignKey(member => member.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(member => member.DisplayOrder)
                .HasDefaultValue(0);
            entity.Property(member => member.EmployeeLastModifiedAtUtc)
                .IsRequired(false);

            entity.HasIndex(member => new { member.AvailabilityGroupId, member.EmployeeId })
                .IsUnique()
                .HasDatabaseName("ux_avail_group_member_group_emp");
        });
    }

    private static void ConfigureAvailabilityGroupDay(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AvailabilityGroupDayModel>(entity =>
        {
            entity.HasOne(day => day.AvailabilityGroupMember)
                .WithMany(member => member.Days)
                .HasForeignKey(day => day.AvailabilityGroupMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(day => day.Kind).HasConversion<string>();

            entity.HasIndex(day => new { day.AvailabilityGroupMemberId, day.DayOfMonth })
                .IsUnique()
                .HasDatabaseName("ux_avail_group_day_member_dom");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_avail_group_day_dom", "day_of_month BETWEEN 1 AND 31");
                table.HasCheckConstraint(
                    "ck_avail_group_day_kind_interval",
                    "((kind = 'INT' AND interval_str IS NOT NULL AND length(trim(interval_str)) >= 11) OR (kind = 'ANY' AND interval_str IS NULL) OR kind = 'NONE')");
            });
        });
    }

    private static void ConfigureAvailabilityGroupDayTransfer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AvailabilityGroupDayTransferModel>(entity =>
        {
            entity.HasOne(transfer => transfer.SourceMember)
                .WithMany()
                .HasForeignKey(transfer => transfer.SourceMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(transfer => transfer.TargetMember)
                .WithMany()
                .HasForeignKey(transfer => transfer.TargetMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(transfer => transfer.CreatedAtUtc).IsRequired();
            entity.HasIndex(transfer => new { transfer.SourceMemberId, transfer.DayOfMonth })
                .IsUnique()
                .HasDatabaseName("ux_avail_transfer_source_day");
            entity.HasIndex(transfer => new { transfer.TargetMemberId, transfer.DayOfMonth })
                .IsUnique()
                .HasDatabaseName("ux_avail_transfer_target_day");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_avail_transfer_dom", "day_of_month BETWEEN 1 AND 31");
                table.HasCheckConstraint("ck_avail_transfer_distinct_members", "source_member_id <> target_member_id");
            });
        });
    }
}
