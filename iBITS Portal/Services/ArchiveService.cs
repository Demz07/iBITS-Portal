using iBITS_Portal.Models;
using Microsoft.EntityFrameworkCore;

namespace iBITS_Portal.Services
{
    public class ArchiveService
    {
        private readonly PortaliBitsContext _context;

        public ArchiveService(PortaliBitsContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Archives all records from a specific semester when creating a new semester
        /// </summary>
        public async Task<ArchiveResult> ArchiveSemesterAsync(int semesterId, string archivedBy, string reason = "Semester Closure")
        {
            var result = new ArchiveResult();

            try
            {
                var semester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.SemesterId == semesterId);

                if (semester == null)
                {
                    result.Success = false;
                    result.Message = "Semester not found";
                    return result;
                }

                var archiveNotes = $"Auto-archived from {semester.AcademicYear.YearName} - {semester.SemesterName}";

                // Archive Students enrolled in this semester
                var studentSemesters = await _context.StudentSemesters
                    .Include(ss => ss.Student)
                    .Where(ss => ss.SemesterId == semesterId)
                    .ToListAsync();

                foreach (var ss in studentSemesters)
                {
                    var student = ss.Student;
                    if (student == null) continue;

                    // Count fees and fines to archive
                    var feesCount = await _context.Fees
                        .Where(f => f.StudentNum == student.StudentNum)
                        .CountAsync();

                    var finesCount = await _context.Fines
                        .Where(f => f.StudentNum == student.StudentNum)
                        .CountAsync();

                    var archivedStudent = new ArchivedStudent
                    {
                        StudentNum = student.StudentNum,
                        StudentFn = student.StudentFn,
                        StudentMn = student.StudentMn,
                        StudentLn = student.StudentLn,
                        Program = student.Course,  // FIXED: Use Course property
                        YearLevel = null,  // FIXED: Not available in Student model
                        Section = student.YearLevelSection,  // FIXED: Use YearLevelSection
                        Email = student.StudentEmail,  // FIXED: Use StudentEmail
                        ContactNum = null,  // FIXED: Not available in Student model
                        ArchivedDate = DateTime.Now,
                        ArchivedBy = archivedBy,
                        ArchiveReason = reason,
                        ArchiveNotes = archiveNotes,
                        SemesterId = semesterId,
                        FeesArchived = feesCount,
                        FinesArchived = finesCount
                    };

                    _context.ArchivedStudents.Add(archivedStudent);
                    result.StudentsArchived++;
                }

                // Archive Fees
                var fees = await _context.Fees.ToListAsync();
                foreach (var fee in fees)
                {
                    var archivedFee = new ArchivedFee
                    {
                        FeeId = fee.FeeId,
                        FeeName = fee.FeeName,
                        StudentNum = fee.StudentNum,
                        Amount = fee.Amount,
                        DueDate = fee.FeesDueDate.HasValue ? fee.FeesDueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,  // FIXED: Convert DateOnly to DateTime
                        CollectionDate = fee.CollectionDate,
                        Status = fee.FeeStatus,  // FIXED: Use FeeStatus
                        AcadYear = fee.AcadYear,
                        ArchivedDate = DateTime.Now,
                        ArchivedBy = archivedBy,
                        ArchiveReason = reason,
                        ArchiveNotes = archiveNotes,
                        SemesterId = semesterId
                    };

                    _context.ArchivedFees.Add(archivedFee);
                    result.FeesArchived++;
                }

                // Archive Fines
                var fines = await _context.Fines.ToListAsync();
                foreach (var fine in fines)
                {
                    var archivedFine = new ArchivedFine
                    {
                        FineId = fine.FineId,
                        StudentNum = fine.StudentNum,
                        EventId = fine.Attendance?.EventId,  // FIXED: Get EventId through Attendance
                        EventName = fine.Attendance?.Event?.EventName,  // FIXED: Get EventName through Attendance->Event
                        Amount = fine.Amount,
                        Reason = fine.Description,  // FIXED: Use Description for manual fines
                        FineDate = fine.FinesStartDate.HasValue ? fine.FinesStartDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,  // FIXED: Convert DateOnly to DateTime
                        CollectionDate = fine.CollectionDate,
                        Status = fine.FinesStatus,  // FIXED: Use FinesStatus
                        ArchivedDate = DateTime.Now,
                        ArchivedBy = archivedBy,
                        ArchiveReason = reason,
                        ArchiveNotes = archiveNotes,
                        SemesterId = semesterId
                    };

                    _context.ArchivedFines.Add(archivedFine);
                    result.FinesArchived++;
                }

                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = $"Successfully archived {result.StudentsArchived} students, {result.FeesArchived} fees, and {result.FinesArchived} fines";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error during archival: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Restore archived data (if needed)
        /// </summary>
        public async Task<ArchiveResult> RestoreArchivedDataAsync(int archivedStudentId, string restoredBy)
        {
            var result = new ArchiveResult();

            try
            {
                var archivedStudent = await _context.ArchivedStudents
                    .FirstOrDefaultAsync(a => a.ArchivedStudentId == archivedStudentId);

                if (archivedStudent == null)
                {
                    result.Success = false;
                    result.Message = "Archived student not found";
                    return result;
                }

                // Restore Student
                var student = new Student
                {
                    StudentNum = archivedStudent.StudentNum,
                    StudentFn = archivedStudent.StudentFn,
                    StudentMn = archivedStudent.StudentMn,
                    StudentLn = archivedStudent.StudentLn,
                    Course = archivedStudent.Program,  // FIXED: Use Course property
                    YearLevelSection = archivedStudent.Section,  // FIXED: Use YearLevelSection
                    StudentEmail = archivedStudent.Email  // FIXED: Use StudentEmail
                    // ContactNum not available in Student model
                };

                _context.Students.Add(student);

                // Restore Fees
                var archivedFees = await _context.ArchivedFees
                    .Where(f => f.StudentNum == archivedStudent.StudentNum && f.SemesterId == archivedStudent.SemesterId)
                    .ToListAsync();

                foreach (var af in archivedFees)
                {
                    var fee = new Fee
                    {
                        FeeName = af.FeeName,
                        StudentNum = af.StudentNum,
                        Amount = af.Amount,
                        FeesDueDate = af.DueDate.HasValue ? DateOnly.FromDateTime(af.DueDate.Value) : (DateOnly?)null,  // FIXED: Convert DateTime to DateOnly
                        CollectionDate = af.CollectionDate,
                        FeeStatus = af.Status,  // FIXED: Use FeeStatus
                        AcadYear = af.AcadYear
                    };
                    _context.Fees.Add(fee);
                    result.FeesRestored++;
                }

                // Restore Fines
                var archivedFines = await _context.ArchivedFines
                    .Where(f => f.StudentNum == archivedStudent.StudentNum && f.SemesterId == archivedStudent.SemesterId)
                    .ToListAsync();

                foreach (var af in archivedFines)
                {
                    var fine = new Fine
                    {
                        StudentNum = af.StudentNum,
                        AttendanceId = null,  // FIXED: Cannot restore EventId directly, use AttendanceId
                        Amount = af.Amount,
                        Description = af.Reason,  // FIXED: Use Description
                        FinesStartDate = af.FineDate.HasValue ? DateOnly.FromDateTime(af.FineDate.Value) : (DateOnly?)null,  // FIXED: Convert DateTime to DateOnly
                        CollectionDate = af.CollectionDate,
                        FinesStatus = af.Status  // FIXED: Use FinesStatus
                    };
                    _context.Fines.Add(fine);
                    result.FinesRestored++;
                }

                await _context.SaveChangesAsync();

                result.Success = true;
                result.StudentsRestored = 1;
                result.Message = $"Successfully restored student with {result.FeesRestored} fees and {result.FinesRestored} fines";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error during restoration: {ex.Message}";
                return result;
            }
        }
    }

    public class ArchiveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StudentsArchived { get; set; }
        public int FeesArchived { get; set; }
        public int FinesArchived { get; set; }
        public int StudentsRestored { get; set; }
        public int FeesRestored { get; set; }
        public int FinesRestored { get; set; }
    }
}
