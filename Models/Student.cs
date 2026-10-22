
using System.ComponentModel.DataAnnotations;

namespace StudentCourseManagement.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        public string? Department { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
