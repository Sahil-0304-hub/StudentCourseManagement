using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentCourseManagement.Data;

namespace StudentCourseManagement.Controllers
{
    [Authorize(Roles = "Admin,Faculty,Student")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.StudentCount = await _context.Students.CountAsync();
            ViewBag.CourseCount = await _context.Courses.CountAsync();
            ViewBag.EnrollmentCount = await _context.Enrollments.CountAsync();

            if (User.IsInRole("Student"))
            {
                var studentEmail = User.Identity?.Name;
                ViewBag.MyEnrollmentCount = await _context.Enrollments
                    .CountAsync(e => e.Student != null && e.Student.Email == studentEmail);
            }

            if (User.IsInRole("Admin"))
            {
                var totalRevenue = await _context.Enrollments
                    .Include(e => e.Course)
                    .Where(e => e.Course != null)
                    .SumAsync(e => (decimal?)e.Course!.Fees) ?? 0;

                ViewBag.TotalRevenue = totalRevenue;
            }

            return View();
        }
    }
}