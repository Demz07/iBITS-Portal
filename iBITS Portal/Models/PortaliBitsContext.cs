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
    
    // ============================================================
    // REMITTANCE SYSTEM DbSets
    // ============================================================
    public virtual DbSet<Remittance> Remittances { get; set; }
    public virtual DbSet<RemittanceItem> RemittanceItems { get; set; }
    
    // ============================================================
    // ANNOUNCEMENT DISMISSAL TRACKING
    // ============================================================
    public virtual DbSet<UserAnnouncementDismissal> UserAnnouncementDismissals { get; set; }
    
    // ============================================================
    // SEMESTER & ACADEMIC YEAR SYSTEM
    // ============================================================
    public virtual DbSet<AcademicYear> AcademicYears { get; set; }
    public virtual DbSet<Semester> Semesters { get; set; }
    public virtual DbSet<StudentSemester> StudentSemesters { get; set; }
    
    // ============================================================
    // ARCHIVE SYSTEM (Fees, Fines, Students)
    // ============================================================
    public virtual DbSet<ArchivedFee> ArchivedFees { get; set; }
    public virtual DbSet<ArchivedFine> ArchivedFines { get; set; }
    public virtual DbSet<ArchivedStudent> ArchivedStudents { get; set; }

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

        // === UPDATE STARTS HERE ===
        modelBuilder.Entity<Event>(entity =>
        {
            // Configure Default Value for IsClosed
            entity.Property(e => e.IsClosed)
                  .HasDefaultValue(false);
        });
        // === UPDATE ENDS HERE ===

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

        // Fee and Fine configurations moved to Remittance System section below



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

        // ============================================================
        // REMITTANCE SYSTEM Configuration
        // ============================================================

        // Remittance Configuration
        modelBuilder.Entity<Remittance>(entity =>
        {
            entity.HasKey(e => e.RemittanceId);
            entity.ToTable("Remittances");

            entity.Property(e => e.BatchCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.FeeName)
                .HasMaxLength(200);

            entity.Property(e => e.FineCategory)
                .HasMaxLength(200);

            entity.Property(e => e.RemittanceType)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Section)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.SubmittedBy)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.SubmittedDate)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.Property(e => e.ValidatedBy)
                .HasMaxLength(450);

            entity.Property(e => e.ValidationNotes)
                .HasMaxLength(1000);

            entity.Property(e => e.RejectionReason)
                .HasMaxLength(1000);

            entity.Property(e => e.AcademicYear)
                .HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Unique constraint on BatchCode
            entity.HasIndex(e => e.BatchCode)
                .IsUnique();

            // Index for filtering by status
            entity.HasIndex(e => e.Status);

            // Index for filtering by section
            entity.HasIndex(e => e.Section);

            // Navigation properties
            entity.HasOne(d => d.SubmittedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.SubmittedBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.ValidatedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.ValidatedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // RemittanceItem Configuration
        modelBuilder.Entity<RemittanceItem>(entity =>
        {
            entity.HasKey(e => e.RemittanceItemId);
            entity.ToTable("RemittanceItems");

            entity.Property(e => e.StudentNum)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.StudentName)
                .HasMaxLength(300);

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50);

            entity.Property(e => e.TransactionRef)
                .HasMaxLength(100);

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            // Index on RemittanceId
            entity.HasIndex(e => e.RemittanceId);

            // Navigation properties
            entity.HasOne(d => d.Remittance)
                .WithMany(p => p.RemittanceItems)
                .HasForeignKey(d => d.RemittanceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Fee)
                .WithMany()
                .HasForeignKey(d => d.FeeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Fine)
                .WithMany()
                .HasForeignKey(d => d.FineId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentNum)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Update Fee entity with Remittance relationship
        modelBuilder.Entity<Fee>(entity =>
        {
            // Existing configuration preserved...
            entity.HasOne(d => d.StudentNumNavigation)
                .WithMany(p => p.Fees)
                .HasForeignKey(d => d.StudentNum)
                .HasConstraintName("FK_Fees_Student")
                .OnDelete(DeleteBehavior.Cascade);

            // Remittance relationship
            entity.HasOne(d => d.Remittance)
                .WithMany()
                .HasForeignKey(d => d.RemittanceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.CollectedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.CollectedBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(e => e.RemittanceStatus)
                .HasMaxLength(20)
                .HasDefaultValue("NotRemitted");

            entity.HasIndex(e => e.RemittanceStatus);
        });

        // Update Fine entity with Remittance relationship
        modelBuilder.Entity<Fine>(entity =>
        {
            // Existing configuration preserved...
            entity.HasOne(d => d.Attendance)
                .WithMany(p => p.Fines)
                .HasForeignKey(d => d.AttendanceId)
                .HasConstraintName("FK_Fines_Attendance")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.StudentNumNavigation)
               .WithMany(p => p.Fines)
               .HasForeignKey(d => d.StudentNum)
               .HasConstraintName("FK_Fines_Student")
               .OnDelete(DeleteBehavior.NoAction);

            // Remittance relationship
            entity.HasOne(d => d.Remittance)
                .WithMany()
                .HasForeignKey(d => d.RemittanceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.CollectedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.CollectedBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(e => e.RemittanceStatus)
                .HasMaxLength(20)
                .HasDefaultValue("NotRemitted");

            entity.HasIndex(e => e.RemittanceStatus);
        });

        // ============================================================
        // USER ANNOUNCEMENT DISMISSAL CONFIGURATION
        // ============================================================
        modelBuilder.Entity<UserAnnouncementDismissal>(entity =>
        {
            entity.ToTable("UserAnnouncementDismissals");
            
            entity.HasKey(e => e.Id);
            
            // Unique constraint to prevent duplicate dismissals
            entity.HasIndex(e => new { e.StudentNum, e.AnnouncementId })
                  .IsUnique()
                  .HasDatabaseName("UQ_StudentAnnouncement");
            
            // Foreign key to Student
            entity.HasOne(d => d.Student)
                  .WithMany()
                  .HasForeignKey(d => d.StudentNum)
                  .HasConstraintName("FK_UserAnnouncementDismissal_Student")
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Foreign key to Announcement
            entity.HasOne(d => d.Announcement)
                  .WithMany()
                  .HasForeignKey(d => d.AnnouncementId)
                  .HasConstraintName("FK_UserAnnouncementDismissal_Announcement")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // SEMESTER & ACADEMIC YEAR RELATIONSHIPS
        // ============================================================
        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasOne(s => s.AcademicYear)
                .WithMany(ay => ay.Semesters)
                .HasForeignKey(s => s.AcademicYearId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentSemester>(entity =>
        {
            entity.HasOne(ss => ss.Semester)
                .WithMany(s => s.StudentSemesters)
                .HasForeignKey(ss => ss.SemesterId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ss => ss.Student)
                .WithMany()
                .HasForeignKey(ss => ss.StudentNum)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}