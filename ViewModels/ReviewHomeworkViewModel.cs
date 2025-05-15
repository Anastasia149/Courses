using Courses.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

public class ReviewHomeworkViewModel
{
    [ValidateNever]
    public Homework Homework { get; set; }

    [Required(ErrorMessage = "Оставьте комментарий")]
    public string Feedback { get; set; }

    [Required]
    public HomeworkStatus Status { get; set; } = HomeworkStatus.Approved;
}