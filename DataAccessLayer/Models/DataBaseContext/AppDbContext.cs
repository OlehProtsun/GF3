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
    public DbSet<ShopModel> Shops => Set<ShopModel>();
    public DbSet<ScheduleModel> Schedules => Set<ScheduleModel>();
    public DbSet<SchedulePresetModel> SchedulePresets => Set<SchedulePresetModel>();
    public DbSet<SchedulePresetEmployeeModel> SchedulePresetEmployees => Set<SchedulePresetEmployeeModel>();
    public DbSet<ScheduleEmployeeModel> ScheduleEmployees => Set<ScheduleEmployeeModel>();
    public DbSet<ScheduleSlotModel> ScheduleSlots => Set<ScheduleSlotModel>();
    public DbSet<ScheduleCellStyleModel> ScheduleCellStyles => Set<ScheduleCellStyleModel>();
    public DbSet<ShiftSwapRequestModel> ShiftSwapRequests => Set<ShiftSwapRequestModel>();
    public DbSet<WorkflowLogEntryModel> WorkflowLogEntries => Set<WorkflowLogEntryModel>();
    public DbSet<BindModel> AvailabilityBinds => Set<BindModel>();
    public DbSet<AvailabilityGroupModel> AvailabilityGroups => Set<AvailabilityGroupModel>();
    public DbSet<AvailabilityGroupMemberModel> AvailabilityGroupMembers => Set<AvailabilityGroupMemberModel>();
    public DbSet<AvailabilityGroupDayModel> AvailabilityGroupDays => Set<AvailabilityGroupDayModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureContainer(modelBuilder);
        ConfigureEmployee(modelBuilder);
        ConfigureEmployeeAccount(modelBuilder);
        ConfigureShop(modelBuilder);
        ConfigureSchedule(modelBuilder);
        ConfigureSchedulePreset(modelBuilder);
        ConfigureSchedulePresetEmployee(modelBuilder);
        ConfigureScheduleEmployee(modelBuilder);
        ConfigureScheduleSlot(modelBuilder);
        ConfigureScheduleCellStyle(modelBuilder);
        ConfigureShiftSwapRequest(modelBuilder);
        ConfigureWorkflowLogEntry(modelBuilder);
        ConfigureAvailabilityBind(modelBuilder);
        ConfigureAvailabilityGroup(modelBuilder);
        ConfigureAvailabilityGroupMember(modelBuilder);
        ConfigureAvailabilityGroupDay(modelBuilder);
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

    private static void ConfigureWorkflowLogEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowLogEntryModel>(entity =>
        {
            entity.Property(log => log.OccurredAtUtc).IsRequired();
            entity.Property(log => log.ActorRole).IsRequired().HasMaxLength(32);
            entity.Property(log => log.ActorName).IsRequired().HasMaxLength(160);
            entity.Property(log => log.Action).IsRequired().HasMaxLength(512);
        });
    }

    private static void ConfigureAvailabilityBind(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BindModel>(entity =>
        {
            entity.Property(bind => bind.Key).IsRequired();
            entity.Property(bind => bind.Value).IsRequired();
            entity.HasIndex(bind => bind.Key).IsUnique();
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
}
