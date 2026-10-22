
using System.ComponentModel.DataAnnotations;

namespace StudentCourseManagement.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        public Student? Student { get; set; }

        [Required]
        public int CourseId { get; set; }

        public Course? Course { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.Today;

        [StringLength(30)]
        public string Status { get; set; } = "Active";
    }
}
