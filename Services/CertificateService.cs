using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Courses.Data;
using Courses.Models;

namespace Courses.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CertificateService> _logger;

        public CertificateService(
            AppDbContext context, 
            INotificationService notificationService,
            ILogger<CertificateService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<bool> IssueCertificateIfEligibleAsync(string studentId, int courseId)
        {
            try
            {
                _logger.LogInformation($"Проверка возможности выдачи сертификата для студента {studentId} по курсу {courseId}");

                // Проверяем, есть ли уже сертификат
                var existingCertificate = await _context.Certificates
                    .AnyAsync(c => c.StudentId == studentId && c.CourseId == courseId);

                if (existingCertificate)
                {
                    _logger.LogInformation($"Сертификат уже существует для студента {studentId} по курсу {courseId}");
                    return false;
                }

                // Проверяем, имеет ли студент право на сертификат
                if (!await IsStudentEligibleForCertificateAsync(studentId, courseId))
                {
                    _logger.LogInformation($"Студент {studentId} не имеет права на сертификат по курсу {courseId}");
                    return false;
                }

                // Получаем курс для уведомления
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    _logger.LogError($"Курс {courseId} не найден");
                    return false;
                }

                // Создаем новый сертификат
                var certificate = new Certificate
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    IssuedAt = DateTime.UtcNow
                };

                _context.Certificates.Add(certificate);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Сертификат успешно выдан для студента {studentId} по курсу {courseId}");

                // Отправляем уведомление студенту
                await _notificationService.CreateNotificationAsync(
                    studentId,
                    "Новый сертификат",
                    $"Поздравляем! Вы получили сертификат за курс \"{course.Title}\"",
                    NotificationType.CertificateIssued
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при выдаче сертификата для студента {studentId} по курсу {courseId}");
                return false;
            }
        }

        public async Task<bool> IsStudentEligibleForCertificateAsync(string studentId, int courseId)
        {
            try
            {
                _logger.LogInformation($"Проверка права на сертификат для студента {studentId} по курсу {courseId}");

                // Получаем все уроки курса
                var courseLessons = await _context.Lessons
                    .Where(l => l.CourseId == courseId)
                    .ToListAsync();

                if (!courseLessons.Any())
                {
                    _logger.LogWarning($"В курсе {courseId} нет уроков");
                    return false;
                }

                _logger.LogInformation($"Найдено {courseLessons.Count} уроков в курсе {courseId}");

                // Проверяем, что все домашние задания выполнены и оценены положительно
                foreach (var lesson in courseLessons)
                {
                    var homework = await _context.Homeworks
                        .FirstOrDefaultAsync(h => h.LessonId == lesson.Id && h.StudentId == studentId);

                    if (homework == null)
                    {
                        _logger.LogWarning($"Нет домашнего задания для урока {lesson.Id} у студента {studentId}");
                        return false;
                    }

                    if (homework.Status != HomeworkStatus.Approved)
                    {
                        _logger.LogWarning($"Домашнее задание для урока {lesson.Id} у студента {studentId} имеет статус {homework.Status}");
                        return false;
                    }
                }

                _logger.LogInformation($"Студент {studentId} имеет право на сертификат по курсу {courseId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке права на сертификат для студента {studentId} по курсу {courseId}");
                return false;
            }
        }
    }
} 