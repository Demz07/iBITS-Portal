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
    // SEMESTER SYSTEM DbSets
    // ============================================================
    public virtual DbSet<AcademicYear> AcademicYears { get; set; }
    public virtual DbSet<Semester> Semesters { get; set; }
    public virtual DbSet<StudentSemester> StudentSemesters { get; set; }
    
    // ============================================================
    // QR AUDIT SYSTEM DbSets
    // ============================================================
    public virtual DbSet<QRAuditLog> QRAuditLogs { get; set; }

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

            // NEW: Semester relationship
            entity.HasOne(d => d.Semester)
                .WithMany()
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_Fines_Semester")
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.RemittanceStatus)
                .HasMaxLength(20)
                .HasDefaultValue("NotRemitted");

            entity.HasIndex(e => e.RemittanceStatus);
        });

        // Configure PaymentTransaction entity
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");

            entity.HasOne(d => d.Fee)
                .WithMany()
                .HasForeignKey(d => d.FeeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentNum)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Treasurer)
                .WithMany()
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // NEW: Semester relationship for single-semester payments
            entity.HasOne(d => d.Semester)
                .WithMany()
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_PaymentTransactions_Semester")
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.SemesterId);
            entity.HasIndex(e => e.StudentNum);
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
        // SEMESTER SYSTEM CONFIGURATION
        // ============================================================
        
        // AcademicYear Configuration
        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.ToTable("AcademicYears");
            
            entity.Property(e => e.YearName)
                .IsRequired()
                .HasMaxLength(20);
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
                
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(450);
                
            // Unique constraint on YearName
            entity.HasIndex(e => e.YearName)
                .IsUnique();
        });

        // Semester Configuration
        modelBuilder.Entity<Semester>(entity =>
        {
            entity.ToTable("Semesters");
            
            entity.Property(e => e.SemesterName)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
                
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(450);
                
            // Unique constraint on AcademicYearId + SemesterName
            entity.HasIndex(e => new { e.AcademicYearId, e.SemesterName })
                .IsUnique();
                
            // Foreign key to AcademicYear
            entity.HasOne(d => d.AcademicYear)
                .WithMany(p => p.Semesters)
                .HasForeignKey(d => d.AcademicYearId)
                .HasConstraintName("FK_Semesters_AcademicYears")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StudentSemester Configuration
        modelBuilder.Entity<StudentSemester>(entity =>
        {
            entity.ToTable("StudentSemesters");
            
            entity.Property(e => e.StudentNum)
                .IsRequired()
                .HasMaxLength(450);
                
            entity.Property(e => e.Section)
                .HasMaxLength(100);
                
            entity.Property(e => e.EnrollmentDate)
                .HasDefaultValueSql("GETDATE()");
                
            entity.Property(e => e.EnrollmentStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Active");
                
            // Unique constraint on StudentNum + SemesterId
            entity.HasIndex(e => new { e.StudentNum, e.SemesterId })
                .IsUnique();
                
            // Foreign key to Student
            entity.HasOne(d => d.Student)
                .WithMany(p => p.StudentSemesters)
                .HasForeignKey(d => d.StudentNum)
                .HasConstraintName("FK_StudentSemesters_Students")
                .OnDelete(DeleteBehavior.Cascade);
                
            // Foreign key to Semester
            entity.HasOne(d => d.Semester)
                .WithMany(p => p.StudentSemesters)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_StudentSemesters_Semesters")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QRAuditLog Configuration
        modelBuilder.Entity<QRAuditLog>(entity =>
        {
            entity.ToTable("QRAuditLog");
            entity.HasKey(e => e.AuditId);
            
            entity.Property(e => e.AuditId).ValueGeneratedOnAdd();
            entity.Property(e => e.StudentNum).IsRequired().HasMaxLength(450);
            entity.Property(e => e.QRCodeData).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ScanTime).HasColumnType("datetime2");
            entity.Property(e => e.ScanType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProcessingResult).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AttendanceId);
            entity.Property(e => e.EventId);
            entity.Property(e => e.SemesterId);
            entity.Property(e => e.DeviceFingerprint).HasMaxLength(200);
            entity.Property(e => e.IPAddress).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.ProcessedBy).HasMaxLength(450);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            // Navigation properties
            entity.HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentNum)
                .HasConstraintName("FK_QRAuditLog_Student")
                .OnDelete(DeleteBehavior.NoAction);
                
            entity.HasOne(d => d.Attendance)
                .WithMany()
                .HasForeignKey(d => d.AttendanceId)
                .HasConstraintName("FK_QRAuditLog_Attendance")
                .OnDelete(DeleteBehavior.NoAction);
                
            entity.HasOne(d => d.Event)
                .WithMany()
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK_QRAuditLog_Event")
                .OnDelete(DeleteBehavior.NoAction);
                
            entity.HasOne(d => d.Semester)
                .WithMany()
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_QRAuditLog_Semester")
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ============================================================
        // UPDATE EXISTING ENTITIES WITH SEMESTER RELATIONSHIPS
        // ============================================================
        
        // Update Attendance with SemesterId
        modelBuilder.Entity<Attendance>(entity =>
        {
            // Add SemesterId column mapping
            entity.Property(e => e.SemesterId);
            
            // Add TimeIn/TimeOut properties
            entity.Property(e => e.TimeIn);
            entity.Property(e => e.TimeOut);
            entity.Property(e => e.DurationMinutes); // Computed column
            entity.Property(e => e.ScanDevice).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(200);
            
            // Add Semester navigation
            entity.HasOne(d => d.Semester)
                .WithMany(p => p.Attendances)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_Attendance_Semesters")
                .OnDelete(DeleteBehavior.SetNull);
                
            // QRAuditLog navigation removed (since QRAuditLog is now keyless)
            // Use separate audit lookup if needed
                
            // Indexes
            entity.HasIndex(e => e.SemesterId);
            entity.HasIndex(e => e.TimeIn);
        });

        // Update Event with SemesterId
        modelBuilder.Entity<Event>(entity =>
        {
            // Add SemesterId column mapping
            entity.Property(e => e.SemesterId);
            
            // Add Semester navigation
            entity.HasOne(d => d.Semester)
                .WithMany(p => p.Events)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK_Events_Semesters")
                .OnDelete(DeleteBehavior.SetNull);
                
            // QRAuditLog navigation removed (keyless entity doesn't support EF navigation)
                
            // Index
            entity.HasIndex(e => e.SemesterId);
        });

        // Update Student with Semester relationships
        modelBuilder.Entity<Student>(entity =>
        {
            // Add StudentSemester navigation
            entity.HasMany(d => d.StudentSemesters)
                .WithOne(p => p.Student)
                .HasForeignKey(p => p.StudentNum)
                .HasConstraintName("FK_StudentSemesters_Students")
                .OnDelete(DeleteBehavior.Cascade);
                
// QRAuditLog navigation removed (keyless entity doesn't support EF navigation)
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}