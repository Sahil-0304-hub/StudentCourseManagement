using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentCourseManagement.Data;
using StudentCourseManagement.Models;

namespace StudentCourseManagement.Controllers
{
    [Authorize(Roles = "Admin,Faculty,Student")]
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var isStudent = User.IsInRole("Student");
            var userEmail = User.Identity?.Name;

            IQueryable<Enrollment> query = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course);

            if (isStudent && !string.IsNullOrEmpty(userEmail))
            {
                query = query.Where(e => e.Student != null && e.Student.Email == userEmail);
            }

            var enrollments = await query
                .OrderByDescending(e => e.Id)
                .ToListAsync();

            ViewBag.IsStudent = isStudent;
            return View(enrollments);
        }

        public async Task<IActionResult> Create(int? courseId = null)
        {
            var isStudent = User.IsInRole("Student");
            ViewBag.IsStudent = isStudent;

            if (isStudent)
            {
                var student = await GetOrCreateCurrentStudentAsync();
                ViewBag.CurrentStudent = student;
                await LoadCourseDropdown(courseId);
                return View(new Enrollment { StudentId = student.Id, CourseId = courseId ?? 0 });
            }

            await LoadDropdowns(null, courseId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Enrollment enrollment)
        {
            var isStudent = User.IsInRole("Student");
            ViewBag.IsStudent = isStudent;

            if (isStudent)
            {
                var currentStudent = await GetOrCreateCurrentStudentAsync();
                enrollment.StudentId = currentStudent.Id;
                ViewBag.CurrentStudent = currentStudent;
            }

            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == enrollment.StudentId);

            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == enrollment.CourseId);

            if (!studentExists || !courseExists)
            {
                ModelState.AddModelError("", "Please select a valid student and course.");
            }

            // Check if student is already enrolled in this course
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == enrollment.StudentId && e.CourseId == enrollment.CourseId);

            if (alreadyEnrolled)
            {
                ModelState.AddModelError("", "This student is already enrolled in this course.");
            }

            if (ModelState.IsValid)
            {
                enrollment.EnrollmentDate = DateTime.Today;
                enrollment.Status = "Active";

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                TempData["Success"] = isStudent
                    ? "You have enrolled in the course successfully!"
                    : "Student enrolled successfully!";

                return RedirectToAction(nameof(Index));
            }

            if (isStudent)
            {
                await LoadCourseDropdown(enrollment.CourseId);
            }
            else
            {
                await LoadDropdowns(enrollment.StudentId, enrollment.CourseId);
            }

            return View(enrollment);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Student"))
            {
                var userEmail = User.Identity?.Name;
                if (enrollment.Student?.Email != userEmail)
                {
                    return Forbid();
                }
            }

            ViewBag.IsStudent = User.IsInRole("Student");
            return View(enrollment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment != null)
            {
                if (User.IsInRole("Student"))
                {
                    var userEmail = User.Identity?.Name;
                    if (enrollment.Student?.Email != userEmail)
                    {
                        return Forbid();
                    }
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Enrollment cancelled successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl = null)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound();
            }

            enrollment.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Enrollment status updated to \"{status}\" successfully!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<Student> GetOrCreateCurrentStudentAsync()
        {
            var userEmail = User.Identity?.Name ?? "student@student.com";
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == userEmail);

            if (student == null)
            {
                student = new Student
                {
                    FullName = userEmail.Split('@')[0],
                    Email = userEmail,
                    Department = "General"
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
            }

            return student;
        }

        private async Task LoadCourseDropdown(int? selectedCourseId = null)
        {
            var courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            ViewBag.CourseId = new SelectList(courses, "Id", "CourseName", selectedCourseId);
        }

        private async Task LoadDropdowns(
            int? selectedStudentId = null,
            int? selectedCourseId = null)
        {
            var students = await _context.Students
                .OrderBy(s => s.FullName)
                .ToListAsync();

            var courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            ViewBag.StudentId = new SelectList(
                students, "Id", "FullName", selectedStudentId);

            ViewBag.CourseId = new SelectList(
                courses, "Id", "CourseName", selectedCourseId);
        }
    }
}
