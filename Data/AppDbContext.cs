using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Models;

namespace QLTTYKPH.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Survey> Surveys { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<FeedbackAnswer> FeedbackAnswers { get; set; }
        public DbSet<ProcessingRecord> ProcessingRecords { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Complaint> Complaints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => v.ToLower() == "admin" ? UserRole.Admin
                       : (v.ToLower() == "staff" || v.ToLower() == "department") ? UserRole.Staff
                       : UserRole.Student
                );

            modelBuilder.Entity<Feedback>()
                .Property(f => f.Status)
                .HasConversion(
                    v => v == FeedbackStatus.Processing ? "InProgress"
                       : v == FeedbackStatus.Completed ? "Resolved"
                       : "New",
                    v => v.ToLower() == "inprogress" ? FeedbackStatus.Processing
                       : v.ToLower() == "resolved" ? FeedbackStatus.Completed
                       : FeedbackStatus.New
                );

            modelBuilder.Entity<Question>()
                .Property(q => q.Type)
                .HasConversion(
                    v => v.ToString(),
                    v => v.ToLower() == "singlechoice" ? QuestionType.SingleChoice
                       : v.ToLower() == "multiplechoice" ? QuestionType.MultipleChoice
                       : v.ToLower() == "rating" ? QuestionType.Rating
                       : QuestionType.Text
                );

            modelBuilder.Entity<Survey>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Surveys)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Survey>()
                .HasOne(s => s.Department)
                .WithMany(d => d.Surveys)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Survey>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Surveys)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Class)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Survey)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Survey)
                .WithMany(s => s.Feedbacks)
                .HasForeignKey(f => f.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackAnswer>()
                .HasOne(fa => fa.Feedback)
                .WithMany(f => f.FeedbackAnswers)
                .HasForeignKey(fa => fa.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeedbackAnswer>()
                .HasOne(fa => fa.Question)
                .WithMany(q => q.FeedbackAnswers)
                .HasForeignKey(fa => fa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProcessingRecord>()
                .HasOne(pr => pr.Feedback)
                .WithMany(f => f.ProcessingRecords)
                .HasForeignKey(pr => pr.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProcessingRecord>()
                .HasOne(pr => pr.HandlerUser)
                .WithMany(u => u.ProcessingRecords)
                .HasForeignKey(pr => pr.HandlerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Complaint>()
                .Property(c => c.Status)
                .HasConversion(
                    v => v == ComplaintStatus.Processing ? "InProgress"
                       : v == ComplaintStatus.Resolved ? "Resolved"
                       : "New",
                    v => v.ToLower() == "inprogress" ? ComplaintStatus.Processing
                       : v.ToLower() == "resolved" ? ComplaintStatus.Resolved
                       : ComplaintStatus.New
                );

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.Department)
                .WithMany()
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.ResolvedByUser)
                .WithMany()
                .HasForeignKey(c => c.ResolvedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
