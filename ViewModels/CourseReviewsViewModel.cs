using Courses.Models;

namespace Courses.ViewModels
{
    public class CourseReviewsViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public List<Review> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool HasUserReview { get; set; }
        public Review? UserReview { get; set; }
    }
}

