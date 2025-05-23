using Courses.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Courses.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Добавляем новые DbSet для курсов
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Homework> Homeworks { get; set; }
        public DbSet<HomeworkFile> HomeworkFiles { get; set; }
        public DbSet<UserCourse> UserCourses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<LessonComment> LessonComments { get; set; }
        public DbSet<Certificate> Certificates { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Настройка связи многие-ко-многим User ↔ Course
            builder.Entity<UserCourse>()
                .HasKey(uc => new { uc.UserId, uc.CourseId });

            builder.Entity<UserCourse>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserCourses)
                .HasForeignKey(uc => uc.UserId);

            builder.Entity<UserCourse>()
                .HasOne(uc => uc.Course)
                .WithMany(c => c.UserCourses)
                .HasForeignKey(uc => uc.CourseId);

            // Настройка связи между Course и Lesson
            builder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId);

            // Настройка связи между Lesson и Homework
            builder.Entity<Homework>()
                .HasOne(h => h.Lesson)
                .WithMany(l => l.Homeworks)
                .HasForeignKey(h => h.LessonId);

            // Настройка связи между User и Homework
            builder.Entity<Homework>()
                .HasOne(h => h.Student)
                .WithMany()
                .HasForeignKey(h => h.StudentId);

            // Настройка связи между Homework и HomeworkFile
            builder.Entity<HomeworkFile>()
                .HasOne(f => f.Homework)
                .WithMany(h => h.Files)
                .HasForeignKey(f => f.HomeworkId);

            // Остальные настройки...
            builder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict); // Чтобы не удалять учителя при удалении курса

            builder.Entity<Homework>()
                .Property(h => h.Status)
                .HasConversion<string>(); // Для хранения enum как строки в БД

            // Настройка для Notification
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId);
        }
    }
}