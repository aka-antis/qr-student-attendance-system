using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<QrToken> QrTokens => Set<QrToken>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Teacher>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(64).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(128).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32).HasDefaultValue("Teacher");
        });

        b.Entity<Student>(e =>
        {
            e.HasIndex(x => x.StudentCode).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(128).IsRequired();
            e.Property(x => x.StudentCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(32);
        });

        b.Entity<QrToken>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique(); // critical: fast + unique token lookup
            e.Property(x => x.Token).HasMaxLength(128).IsRequired();
            e.HasOne(x => x.Student)
                .WithMany(s => s.QrTokens)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.StudentId, x.IsActive });
        });

        b.Entity<Meeting>(e =>
        {
            e.Property(x => x.CourseName).HasMaxLength(128).IsRequired();
            e.HasOne(x => x.CreatedByTeacher)
                .WithMany(t => t.CreatedMeetings)
                .HasForeignKey(x => x.CreatedByTeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<AttendanceRecord>(e =>
        {
            // DB-level duplicate prevention: one row per Student + Meeting.
            e.HasIndex(x => new { x.StudentId, x.MeetingId }).IsUnique();
            e.HasOne(x => x.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Meeting)
                .WithMany(m => m.Attendances)
                .HasForeignKey(x => x.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ScannedByTeacher)
                .WithMany(t => t.Scans)
                .HasForeignKey(x => x.ScannedByTeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
