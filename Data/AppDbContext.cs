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

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Автоматически удаляем комментарии при удалении задания
            var deletedHomeworks = ChangeTracker.Entries<Homework>()
                .Where(e => e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in deletedHomeworks)
            {
                var homeworkId = entry.Property("Id").CurrentValue;
                if (homeworkId != null)
                {
                    var commentsToDelete = await HomeworkComments
                        .Where(c => c.HomeworkId == (int)homeworkId)
                        .ToListAsync(cancellationToken);
                    
                    if (commentsToDelete.Any())
                    {
                        HomeworkComments.RemoveRange(commentsToDelete);
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        // Добавляем новые DbSet для курсов
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Homework> Homeworks { get; set; }
        public DbSet<HomeworkFile> HomeworkFiles { get; set; }
        public DbSet<UserCourse> UserCourses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<HomeworkComment> HomeworkComments { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CourseCategory> CourseCategories { get; set; }

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
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связи между Lesson и Module (опционально)
            builder.Entity<Lesson>()
                .HasOne(l => l.Module)
                .WithMany(m => m.Lessons)
                .HasForeignKey(l => l.ModuleId)
                .OnDelete(DeleteBehavior.NoAction);

            // Настройка связи Course -> Module
            builder.Entity<Module>()
                .HasOne(m => m.Course)
                .WithMany(c => c.Modules)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // Настройка связи между Course и Module
            builder.Entity<Module>()
                .HasOne(m => m.Course)
                .WithMany(c => c.Modules)
                .HasForeignKey(m => m.CourseId);

            // Настройка связи между Course и Review
            builder.Entity<Review>()
                .HasOne(r => r.Course)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CourseId);

            // Настройка связи между User и Review
            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId);

            // Настройка связи между Course и CourseCategory
            builder.Entity<Course>()
                .HasOne(c => c.Category)
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка связи между Homework и HomeworkComment
            // Удаление комментариев обрабатывается в SaveChangesAsync
            builder.Entity<HomeworkComment>()
                .HasOne(c => c.Homework)
                .WithMany(h => h.Comments)
                .HasForeignKey(c => c.HomeworkId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка связи между User и HomeworkComment
            builder.Entity<HomeworkComment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId);
        }
    }
}