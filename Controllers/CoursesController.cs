using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentCourseManagement.Data;
using StudentCourseManagement.Models;

namespace StudentCourseManagement.Controllers
{
    [Authorize(Roles = "Admin,Faculty,Student")]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display and search courses
        public async Task<IActionResult> Index(string? searchString)
        {
            var courses = _context.Courses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                courses = courses.Where(c =>
                    c.CourseName.Contains(searchString) ||
                    (c.Description != null &&
                     c.Description.Contains(searchString)));
            }

            ViewBag.SearchString = searchString;

            if (User.IsInRole("Student"))
            {
                var userEmail = User.Identity?.Name;
                var enrolledIds = await _context.Enrollments
                    .Where(e => e.Student != null && e.Student.Email == userEmail)
                    .Select(e => e.CourseId)
                    .ToListAsync();
                ViewBag.EnrolledCourseIds = enrolledIds;
            }

            var result = await courses
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(result);
        }

        // Show course roster (Enrolled students) for Faculty & Admin
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Roster(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == id)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync();

            ViewBag.Course = course;
            return View(enrollments);
        }

        // Show add course form
        [Authorize(Roles = "Admin,Faculty")]
        public IActionResult Create()
        {
            return View();
        }

        // Save new course
        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            if (!ModelState.IsValid)
            {
                return View(course);
            }

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course added successfully!";

            return RedirectToAction(nameof(Index));
        }

        // Show edit form
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // Save course changes
        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (id != course.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(course);
            }

            var existingCourse = await _context.Courses.FindAsync(id);

            if (existingCourse == null)
            {
                return NotFound();
            }

            existingCourse.CourseName = course.CourseName;
            existingCourse.Description = course.Description;
            existingCourse.Fees = course.Fees;
            existingCourse.DurationInWeeks = course.DurationInWeeks;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Course updated successfully!";

            return RedirectToAction(nameof(Index));
        }

        // Show delete confirmation
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // Delete course
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Course deleted successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}
