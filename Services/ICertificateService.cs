using System.Threading.Tasks;
using Courses.Models;

namespace Courses.Services
{
    public interface ICertificateService
    {
        Task<bool> IssueCertificateIfEligibleAsync(string studentId, int courseId);
        Task<bool> IsStudentEligibleForCertificateAsync(string studentId, int courseId);
    }
} 