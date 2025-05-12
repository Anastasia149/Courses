using Courses.Models;
using System.ComponentModel.DataAnnotations;

public class ReviewHomeworkViewModel
{
    public Homework Homework { get; set; }

    [Required(ErrorMessage = "Оставьте комментарий")]
    public string Feedback { get; set; }

    [Required]
    public HomeworkStatus Status { get; set; } = HomeworkStatus.Approved;
}