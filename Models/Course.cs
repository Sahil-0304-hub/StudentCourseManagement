
using System.ComponentModel.DataAnnotations;

namespace StudentCourseManagement.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string CourseName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0, 1000000)]
        public decimal Fees { get; set; }

        [Range(1, 1000)]
        public int DurationInWeeks { get; set; }
    }
}
