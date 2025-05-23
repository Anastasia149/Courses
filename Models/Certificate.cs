using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Courses.Models
{
    public class Certificate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }
        public User Student { get; set; }

        [Required]
        public int CourseId { get; set; }
        public Course Course { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        // Можно добавить поле для PDF или номера сертификата
    }
}