using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Models.DataBaseContext;

/// <summary>
/// Central EF Core model for the whole application.
/// The context does two jobs:
/// 1. exposes aggregate roots as <see cref="DbSet{TEntity}"/>,
/// 2. defines relational invariants that must stay true regardless of which API/service writes data.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ContainerModel> Containers => Set<ContainerModel>();
    public DbSet<EmployeeModel> Employees => Set<EmployeeModel>();
    public DbSet<EmployeeAccountModel> EmployeeAccounts => Set<EmployeeAccountModel>();
    public DbSet<ManagerAccountModel> ManagerAccounts => Set<ManagerAccountModel>();
    public DbSet<ManagerNoteModel> ManagerNotes => Set<ManagerNoteModel>();
    public DbSet<ManagerNotepadStateModel> ManagerNotepadStates => Set<ManagerNotepadStateModel>();
    public DbSet<ManagerGraphFillColorBindModel> ManagerGraphFillColorBinds => Set<ManagerGraphFillColorBindModel>();
    public DbSet<ManagerGraphTextColorBindModel> ManagerGraphTextColorBinds => Set<ManagerGraphTextColorBindModel>();
    public DbSet<SystemNewsMessageModel> SystemNewsMessages => Set<SystemNewsMessageModel>();
    public DbSet<SystemNewsReadModel> SystemNewsReads => Set<SystemNewsReadModel>();
    public DbSet<CommunicationMessageModel> CommunicationMessages => Set<CommunicationMessageModel>();
    public DbSet<EmployeeCommunicationDismissalModel> EmployeeCommunicationDismissals => Set<EmployeeCommunicationDismissalModel>();
    public DbSet<EmployeeScheduleColumnPreferenceModel> EmployeeScheduleColumnPreferences => Set<EmployeeScheduleColumnPreferenceModel>();
    public DbSet<EmployeeNotificationReadModel> EmployeeNotificationReads => Set<EmployeeNotificationReadModel>();
    public DbSet<EmployeePinnedSwapModel> EmployeePinnedSwaps => Set<EmployeePinnedSwapModel>();
    public DbSet<ShopModel> Shops => Set<ShopModel>();
    public DbSet<ScheduleModel> Schedules => Set<ScheduleModel>();
    public DbSet<SchedulePresetModel> SchedulePresets => Set<SchedulePresetModel>();
    public DbSet<SchedulePresetEmployeeModel> SchedulePresetEmployees => Set<SchedulePresetEmployeeModel>();
    public DbSet<ScheduleEmployeeModel> ScheduleEmployees => Set<ScheduleEmployeeModel>();
    public DbSet<ScheduleSlotModel> ScheduleSlots => Set<ScheduleSlotModel>();
    public DbSet<ScheduleCellStyleModel> ScheduleCellStyles => Set<ScheduleCellStyleModel>();
    public DbSet<ShiftSwapRequestModel> ShiftSwapRequests => Set<ShiftSwapRequestModel>();
    public DbSet<ShiftSwapHistoryModel> ShiftSwapHistories => Set<ShiftSwapHistoryModel>();
    public DbSet<WorkflowLogEntryModel> WorkflowLogEntries => Set<WorkflowLogEntryModel>();
    public DbSet<WorkflowLogSettingsModel> WorkflowLogSettings => Set<WorkflowLogSettingsModel>();
    public DbSet<RegulationDocumentModel> RegulationDocuments => Set<RegulationDocumentModel>();
    public DbSet<RegulationAcceptanceModel> RegulationAcceptances => Set<RegulationAcceptanceModel>();
    public DbSet<BindModel> AvailabilityBinds => Set<BindModel>();
    public DbSet<AvailabilityGroupModel> AvailabilityGroups => Set<AvailabilityGroupModel>();
    public DbSet<AvailabilityGroupMemberModel> AvailabilityGroupMembers => Set<AvailabilityGroupMemberModel>();
    public DbSet<AvailabilityGroupDayModel> AvailabilityGroupDays => Set<AvailabilityGroupDayModel>();
    public DbSet<AvailabilityGroupDayTransferModel> AvailabilityGroupDayTransfers => Set<AvailabilityGroupDayTransferModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureContainer(modelBuilder);
        ConfigureEmployee(modelBuilder);
        ConfigureEmployeeAccount(modelBuilder);
        ConfigureManagerAccount(modelBuilder);
        ConfigureManagerNote(modelBuilder);
        ConfigureManagerNotepadState(modelBuilder);
        ConfigureManagerGraphFillColorBind(modelBuilder);
        ConfigureManagerGraphTextColorBind(modelBuilder);
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
        ConfigureShiftSwapRequest(modelBuilder);
        ConfigureShiftSwapHistory(modelBuilder);
        ConfigureWorkflowLogEntry(modelBuilder);
        ConfigureWorkflowLogSettings(modelBuilder);
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
        modelBuilder.Entity<EmployeeModel>(entity =>
        {
            entity.Property(property => property.FirstName).IsRequired();
            entity.Property(property => property.LastName).IsRequired();
            entity.Property(property => property.Email).IsRequired(false);
            entity.HasIndex(property => new { property.FirstName, property.LastName })
                .IsUnique()
                .HasDatabaseName("ux_employee_full_name");

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
                .OnDelete(DeleteBehavior.Cascade);

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

    private static void ConfigureShiftSwapRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShiftSwapRequestModel>(entity =>
        {
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
