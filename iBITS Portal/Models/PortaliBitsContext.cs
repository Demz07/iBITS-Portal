using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace iBITS_Portal.Models;

public partial class PortaliBitsContext : DbContext
{
    public PortaliBitsContext()
    {
    }

    public PortaliBitsContext(DbContextOptions<PortaliBitsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }
    public virtual DbSet<Event> Events { get; set; }
    public virtual DbSet<Fee> Fees { get; set; }
    public virtual DbSet<Fine> Fines { get; set; }
    public virtual DbSet<Officer> Officers { get; set; }
    public virtual DbSet<Student> Students { get; set; }
    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }
    public virtual DbSet<Announcement> Announcements { get; set; }
    // REMOVED: public virtual DbSet<ExcuseRequest> ExcuseRequests { get; set; }
    public virtual DbSet<ArchivedEvent> ArchivedEvents { get; set; }
    public virtual DbSet<ArchivedAnnouncement> ArchivedAnnouncements { get; set; }
    public virtual DbSet<PendingRoleChange> PendingRoleChanges { get; set; }
    public virtual DbSet<SystemSetting> SystemSettings { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public virtual DbSet<FinePaymentTransaction> FinePaymentTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>().ToTable("Student");
        modelBuilder.Entity<Event>().ToTable("Event");
        modelBuilder.Entity<Attendance>().ToTable("Attendance");
        // REMOVED: modelBuilder.Entity<ExcuseRequest>().ToTable("ExcuseRequest");
        modelBuilder.Entity<Fee>().ToTable("Fees");
        modelBuilder.Entity<Fine>().ToTable("Fines");
        modelBuilder.Entity<Officer>().ToTable("Officers");
        modelBuilder.Entity<ActivityLog>().ToTable("ActivityLogs");
        modelBuilder.Entity<Announcement>().ToTable("Announcements");
        modelBuilder.Entity<ArchivedEvent>().ToTable("ArchivedEvents");
        modelBuilder.Entity<ArchivedAnnouncement>().ToTable("ArchivedAnnouncements");
        modelBuilder.Entity<PendingRoleChange>().ToTable("PendingRoleChanges");
        modelBuilder.Entity<SystemSetting>().ToTable("SystemSettings");
        modelBuilder.Entity<Notification>().ToTable("Notification");

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasOne(d => d.Event)
                .WithMany(p => p.Attendances)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK_Attendance_Event")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.StudentNumNavigation)
                .WithMany(p => p.Attendances)
                .HasForeignKey(d => d.StudentNum)
                .HasConstraintName("FK_Attendance_Student")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Fee>(entity =>
        {
            entity.HasOne(d => d.StudentNumNavigation)
                .WithMany(p => p.Fees)
                .HasForeignKey(d => d.StudentNum)
                .HasConstraintName("FK_Fees_Student")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Fine>(entity =>
        {
            entity.HasOne(d => d.Attendance)
                .WithMany(p => p.Fines)
                .HasForeignKey(d => d.AttendanceId)
                .HasConstraintName("FK_Fines_Attendance")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // REMOVED: ExcuseRequest configuration block

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasOne(d => d.Officer)
                .WithMany(p => p.Students)
                .HasForeignKey(d => d.OfficerId)
                .HasConstraintName("Fk_Officer")
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PendingRoleChange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StudentNumber).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OldRole).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NewRole).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AssignedByAdminId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.AssignedByAdminName).HasMaxLength(256);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.StudentNum).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.SentBy).HasMaxLength(100);

            entity.HasOne(d => d.StudentNumNavigation)
                .WithMany()
                .HasForeignKey(d => d.StudentNum)
                .HasConstraintName("FK_Notification_Student")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Payment Transaction Configuration
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.ToTable("PaymentTransactions");

            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne(d => d.Fee)
                .WithMany()
                .HasForeignKey(d => d.FeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentNum)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Treasurer)
                .WithMany()
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.FeeId);
            entity.HasIndex(e => e.StudentNum);
            entity.HasIndex(e => e.PaymentDate);
        });

        // Fine Payment Transaction Configuration
        modelBuilder.Entity<FinePaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.FineTransactionId);
            entity.ToTable("FinePaymentTransactions");

            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne(d => d.Fine)
                .WithMany()
                .HasForeignKey(d => d.FineId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentNum)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Treasurer)
                .WithMany()
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.FineId);
            entity.HasIndex(e => e.StudentNum);
            entity.HasIndex(e => e.PaymentDate);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}