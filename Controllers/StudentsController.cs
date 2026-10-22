using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentCourseManagement.Data;
using StudentCourseManagement.Models;

namespace StudentCourseManagement.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display and search students (Admin & Faculty only)
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Index(string? searchString)
        {
            var students = _context.Students.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                students = students.Where(s =>
                    s.FullName.Contains(searchString) ||
                    s.Email.Contains(searchString) ||
                    (s.Department != null &&
                     s.Department.Contains(searchString)));
            }

            ViewBag.SearchString = searchString;

            var result = await students
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            return View(result);
        }

        // Show registration form
        [Authorize(Roles = "Admin,Faculty")]
        public IActionResult Create()
        {
            return View();
        }

        // Save new student
        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (!ModelState.IsValid)
            {
                return View(student);
            }

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student registered successfully!";

            return RedirectToAction(nameof(Index));
        }

        // Show edit form
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // Save student changes
        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(student);
            }

            var existingStudent = await _context.Students.FindAsync(id);

            if (existingStudent == null)
            {
                return NotFound();
            }

            existingStudent.FullName = student.FullName;
            existingStudent.Email = student.Email;
            existingStudent.Phone = student.Phone;
            existingStudent.Department = student.Department;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Student updated successfully!";

            return RedirectToAction(nameof(Index));
        }

        // Show delete confirmation
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // Delete student
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Student deleted successfully!";

            return RedirectToAction(nameof(Index));
        }

        // Student-specific: My Academic Profile
        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> MyProfile()
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
                    Department = "Computer Science"
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
            }

            var myEnrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == student.Id)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync();

            ViewBag.Enrollments = myEnrollments;
            return View(student);
        }

        // Student-specific: Update own contact info
        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMyProfile(Student model)
        {
            var userEmail = User.Identity?.Name;
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == userEmail);

            if (student == null)
            {
                return NotFound();
            }

            student.FullName = model.FullName;
            student.Phone = model.Phone;
            student.Department = model.Department;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Your academic profile has been updated successfully!";

            return RedirectToAction(nameof(MyProfile));
        }
    }
}
