using DataAccessLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Models.DataBaseContext
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<ContainerModel> Containers => Set<ContainerModel>();
        public DbSet<EmployeeModel> Employees => Set<EmployeeModel>();
        public DbSet<ShopModel> Shops => Set<ShopModel>();
        public DbSet<ScheduleModel> Schedules => Set<ScheduleModel>();
        public DbSet<ScheduleEmployeeModel> ScheduleEmployees => Set<ScheduleEmployeeModel>();
        public DbSet<ScheduleSlotModel> ScheduleSlots => Set<ScheduleSlotModel>();
        public DbSet<ScheduleCellStyleModel> ScheduleCellStyles => Set<ScheduleCellStyleModel>();
        public DbSet<BindModel> AvailabilityBinds => Set<BindModel>();
        public DbSet<AvailabilityGroupModel> AvailabilityGroups => Set<AvailabilityGroupModel>();
        public DbSet<AvailabilityGroupMemberModel> AvailabilityGroupMembers => Set<AvailabilityGroupMemberModel>();
        public DbSet<AvailabilityGroupDayModel> AvailabilityGroupDays => Set<AvailabilityGroupDayModel>();



        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        {
            // ----- Container
            modelBuilder.Entity<ContainerModel>(e =>
            {
                e.Property(p => p.Name).IsRequired();
                e.HasIndex(p => p.Name).IsUnique();
            });

            // ----- Employee
            modelBuilder.Entity<EmployeeModel>(e =>
            {
                e.Property(p => p.FirstName).IsRequired();
                e.Property(p => p.LastName).IsRequired();
                e.HasIndex(p => new { p.FirstName, p.LastName })
                    .IsUnique()
                    .HasDatabaseName("ux_employee_full_name");
            });

            // ----- Shop
            modelBuilder.Entity<ShopModel>(e =>
            {
                e.Property(p => p.Name).IsRequired();
                e.Property(p => p.Address).IsRequired();
                e.Property(p => p.Description).IsRequired(false);
                e.HasIndex(p => p.Name)
                    .IsUnique()
                    .HasDatabaseName("ux_shop_name");
            });

            // ----- Schedule
            modelBuilder.Entity<ScheduleModel>(e =>
            {
                e.HasOne(s => s.Container)
                 .WithMany(c => c.Schedules)
                 .HasForeignKey(s => s.ContainerId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(s => s.Shop)
                 .WithMany(c => c.Schedules)
                 .HasForeignKey(s => s.ShopId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(s => s.ContainerId).HasDatabaseName("ix_sched_container");
                e.HasIndex(s => new { s.ShopId, s.Year, s.Month }).HasDatabaseName("ix_sched_shop_month");
                e.HasIndex(s => new { s.ContainerId, s.ShopId }).HasDatabaseName("ix_sched_container_shop");

                e.Property(s => s.Note).IsRequired(false);

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_schedule_month", "month BETWEEN 1 AND 12");
                    t.HasCheckConstraint("ck_schedule_people_per_shift", "people_per_shift >= 1");
                    t.HasCheckConstraint("ck_schedule_max_hours_per_emp_month", "max_hours_per_emp_month >= 0");
                    t.HasCheckConstraint("ck_schedule_max_consecutive_days", "max_consecutive_days >= 1");
                    t.HasCheckConstraint("ck_schedule_max_consecutive_full", "max_consecutive_full >= 1");
                    t.HasCheckConstraint("ck_schedule_max_full_per_month", "max_full_per_month >= 0");
                    t.HasCheckConstraint("ck_schedule_shift1_format", "shift1_time LIKE '__:__ - __:__'");
                    t.HasCheckConstraint("ck_schedule_shift2_format", "shift2_time LIKE '__:__ - __:__'");
                });
                // тригери в БД додатково перевіряють коректність часу зміни

                // AppDbContext.cs -> modelBuilder.Entity<ScheduleModel>(e => { ... })

                e.HasOne(s => s.AvailabilityGroup)
                 .WithMany()
                 .HasForeignKey(s => s.AvailabilityGroupId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(s => s.AvailabilityGroupId).HasDatabaseName("ix_sched_avail_group");

            });

            // ----- ScheduleEmployee
            modelBuilder.Entity<ScheduleEmployeeModel>(e =>
            {
                e.HasOne(se => se.Schedule)
                 .WithMany(s => s.Employees)
                 .HasForeignKey(se => se.ScheduleId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(se => se.Employee)
                 .WithMany(emp => emp.ScheduleEmployees)
                 .HasForeignKey(se => se.EmployeeId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(se => new { se.ScheduleId, se.EmployeeId }).IsUnique();
            });

            // ----- ScheduleSlot
            modelBuilder.Entity<ScheduleSlotModel>(e =>
            {
                e.HasOne(sl => sl.Schedule)
                 .WithMany(s => s.Slots)
                 .HasForeignKey(sl => sl.ScheduleId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(sl => sl.Employee)
                 .WithMany(emp => emp.ScheduleSlots)
                 .HasForeignKey(sl => sl.EmployeeId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.Property(sl => sl.Status)
                 .HasConversion<string>()
                 .HasDefaultValue(SlotStatus.UNFURNISHED);

                e.Property(sl => sl.FromTime).IsRequired();
                e.Property(sl => sl.ToTime).IsRequired();

                e.HasIndex(sl => new { sl.ScheduleId, sl.DayOfMonth, sl.FromTime, sl.ToTime, sl.SlotNo })
                 .IsUnique();

                e.HasIndex(sl => new { sl.ScheduleId, sl.DayOfMonth, sl.FromTime, sl.ToTime, sl.EmployeeId })
                 .IsUnique()
                 .HasDatabaseName("ux_slot_unique_emp_per_time")
                 .HasFilter("employee_id IS NOT NULL");

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_schedule_slot_dom", "day_of_month BETWEEN 1 AND 31");
                    t.HasCheckConstraint("ck_schedule_slot_slot_no", "slot_no >= 1");
                    t.HasCheckConstraint(
                        "ck_schedule_slot_status_pair",
                        "((status='UNFURNISHED' AND employee_id IS NULL) OR (status='ASSIGNED' AND employee_id IS NOT NULL))"
                    );
                    t.HasCheckConstraint(
                        "ck_schedule_slot_time_format",
                        "from_time LIKE '__:__' AND to_time LIKE '__:__'"
                    );
                    t.HasCheckConstraint(
                        "ck_schedule_slot_time_order",
                        "from_time < to_time"
                    );
                });


            });

            // ----- ScheduleCellStyle
            modelBuilder.Entity<ScheduleCellStyleModel>(e =>
            {
                e.HasOne(cs => cs.Schedule)
                 .WithMany(s => s.CellStyles)
                 .HasForeignKey(cs => cs.ScheduleId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(cs => cs.Employee)
                 .WithMany()
                 .HasForeignKey(cs => cs.EmployeeId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(cs => new { cs.ScheduleId, cs.DayOfMonth, cs.EmployeeId })
                 .IsUnique();

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_schedule_cell_style_dom", "day_of_month BETWEEN 1 AND 31");
                });
            });

            // ----- ScheduleCellStyle
            modelBuilder.Entity<ScheduleCellStyleModel>(e =>
            {
                e.HasOne(sc => sc.Schedule)
                 .WithMany(s => s.CellStyles)
                 .HasForeignKey(sc => sc.ScheduleId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(sc => new { sc.ScheduleId, sc.DayOfMonth, sc.EmployeeId })
                 .IsUnique()
                 .HasDatabaseName("ux_sched_cell_style");
            });

            // ----- AvailabilityBind
            modelBuilder.Entity<BindModel>(e =>
            {
                e.Property(x => x.Key).IsRequired();
                e.Property(x => x.Value).IsRequired();
                e.HasIndex(x => x.Key).IsUnique();   // щоб не було двох однакових хоткеїв
            });

            // ----- AvailabilityGroup
            modelBuilder.Entity<AvailabilityGroupModel>(e =>
            {
                e.Property(x => x.Name).IsRequired();
                e.HasIndex(x => new { x.Year, x.Month, x.Name })
                    .IsUnique()
                    .HasDatabaseName("ux_avail_group_year_month_name");

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_availability_group_month", "month BETWEEN 1 AND 12");
                });
            });

            // ----- AvailabilityGroupMember
            modelBuilder.Entity<AvailabilityGroupMemberModel>(e =>
            {
                e.HasOne(m => m.AvailabilityGroup)
                 .WithMany(g => g.Members)
                 .HasForeignKey(m => m.AvailabilityGroupId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.Employee)
                 .WithMany() // або зробиш Employee.AvailabilityGroupMembers
                 .HasForeignKey(m => m.EmployeeId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(m => new { m.AvailabilityGroupId, m.EmployeeId })
                 .IsUnique()
                 .HasDatabaseName("ux_avail_group_member_group_emp");
            });

            // ----- AvailabilityGroupDay
            modelBuilder.Entity<AvailabilityGroupDayModel>(e =>
            {
                e.HasOne(d => d.AvailabilityGroupMember)
                 .WithMany(m => m.Days)
                 .HasForeignKey(d => d.AvailabilityGroupMemberId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(d => d.Kind).HasConversion<string>();

                e.HasIndex(d => new { d.AvailabilityGroupMemberId, d.DayOfMonth })
                 .IsUnique()
                 .HasDatabaseName("ux_avail_group_day_member_dom");

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_avail_group_day_dom", "day_of_month BETWEEN 1 AND 31");
                    t.HasCheckConstraint(
                        "ck_avail_group_day_kind_interval",
                        "((kind = 'INT' AND interval_str IS NOT NULL AND length(trim(interval_str)) >= 11) OR (kind IN ('ANY','NONE') AND interval_str IS NULL))"
                    );
                });
            });



        }
    }
}
