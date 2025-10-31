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
        public DbSet<ShopModel> Shops => Set<ShopModel>();
        public DbSet<EmployeeModel> Employees => Set<EmployeeModel>();
        public DbSet<AvailabilityMonthModel> AvailabilityMonths => Set<AvailabilityMonthModel>();
        public DbSet<AvailabilityDayModel> AvailabilityDays => Set<AvailabilityDayModel>();
        public DbSet<ScheduleModel> Schedules => Set<ScheduleModel>();
        public DbSet<ScheduleEmployeeModel> ScheduleEmployees => Set<ScheduleEmployeeModel>();
        public DbSet<ScheduleSlotModel> ScheduleSlots => Set<ScheduleSlotModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) 
        {
            // ----- Container
            modelBuilder.Entity<ContainerModel>(e =>
            {
                e.Property(p => p.Name).IsRequired();
                e.HasIndex(p => p.Name).IsUnique();
            });

            // ----- Shop
            modelBuilder.Entity<ShopModel>(e =>
            {
                e.Property(p => p.Name).IsRequired();
            });

            // ----- Employee
            modelBuilder.Entity<EmployeeModel>(e =>
            {
                e.Property(p => p.FirstName).IsRequired();
                e.Property(p => p.LastName).IsRequired();
            });

            // ----- AvailabilityMonth
            modelBuilder.Entity<AvailabilityMonthModel>(e =>
            {
                e.HasOne(m => m.Employee)
                 .WithMany(emp => emp.AvailabilityMonths)
                 .HasForeignKey(m => m.EmployeeId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(m => new { m.EmployeeId, m.Year, m.Month })
                 .IsUnique()
                 .HasDatabaseName("ux_availability_month_emp_year_month");

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_availability_month_month", "month BETWEEN 1 AND 12");
                });                    
            });

            // ----- AvailabilityDay
            modelBuilder.Entity<AvailabilityDayModel>(e =>
            {
                e.HasOne(d => d.AvailabilityMonth)
                 .WithMany(m => m.Days)
                 .HasForeignKey(d => d.AvailabilityMonthId)
                 .OnDelete(DeleteBehavior.Cascade);

                // enum <-> TEXT
                e.Property(d => d.Kind).HasConversion<string>();

                e.HasIndex(d => new { d.AvailabilityMonthId, d.DayOfMonth })
                 .IsUnique()
                 .HasDatabaseName("ux_availability_day_month_day");

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_availability_day_dom", "day_of_month BETWEEN 1 AND 31");
                    t.HasCheckConstraint(
                        "ck_availability_day_kind_interval",
                        "((kind = 'INT' AND interval_str IS NOT NULL AND length(trim(interval_str)) >= 11) OR (kind IN ('ANY','NONE') AND interval_str IS NULL))"
                    );
                });
            });

            // ----- Schedule
            modelBuilder.Entity<ScheduleModel>(e =>
            {
                e.HasOne(s => s.Container)
                 .WithMany(c => c.Schedules)
                 .HasForeignKey(s => s.ContainerId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(s => s.Shop)
                 .WithMany(sh => sh.Schedules)
                 .HasForeignKey(s => s.ShopId)
                 .OnDelete(DeleteBehavior.Cascade);

                // enum <-> TEXT
                e.Property(s => s.Status).HasConversion<string>();

                e.HasIndex(s => s.ContainerId).HasDatabaseName("ix_sched_container");
                e.HasIndex(s => new { s.ShopId, s.Year, s.Month }).HasDatabaseName("ix_sched_shop_month");
                e.HasIndex(s => new { s.ContainerId, s.ShopId }).HasDatabaseName("ix_sched_container_shop");

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

                // enum <-> TEXT
                e.Property(sl => sl.Status)
                 .HasConversion<string>()
                 .HasDefaultValue(SlotStatus.UNFURNISHED);

                e.HasIndex(sl => new { sl.ScheduleId, sl.DayOfMonth, sl.ShiftNo, sl.SlotNo })
                 .IsUnique();

                // partial unique index: один співробітник не може двічі бути на тій самій зміні дня
                e.HasIndex(sl => new { sl.ScheduleId, sl.DayOfMonth, sl.ShiftNo, sl.EmployeeId })
                 .IsUnique()
                 .HasDatabaseName("ux_slot_unique_emp_per_shift")
                 .HasFilter("employee_id IS NOT NULL");

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_schedule_slot_dom", "day_of_month BETWEEN 1 AND 31");
                    t.HasCheckConstraint("ck_schedule_slot_shift_no", "shift_no IN (1,2)");
                    t.HasCheckConstraint("ck_schedule_slot_slot_no", "slot_no >= 1");
                    t.HasCheckConstraint(
                        "ck_schedule_slot_status_pair",
                        "((status='UNFURNISHED' AND employee_id IS NULL) OR (status='ASSIGNED' AND employee_id IS NOT NULL))"
                    );
                });
            });
        }
    }
}
