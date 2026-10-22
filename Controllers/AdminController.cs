using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentCourseManagement.Data;
using StudentCourseManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace StudentCourseManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var allowedRoles = new[] { "Admin", "Faculty", "Student" };
            if (!allowedRoles.Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Please select a valid role (Admin, Faculty, or Student).");
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email already exists.");
                return View(model);
            }

            // Ensure the role exists
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            // If creating a Student account, ensure a matching record exists in the Students table
            if (model.Role == "Student")
            {
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email == model.Email);

                if (existingStudent == null)
                {
                    _context.Students.Add(new Student
                    {
                        FullName = model.FullName ?? model.Email.Split('@')[0],
                        Email = model.Email,
                        Department = "General"
                    });
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = $"{model.Role} account created successfully for {model.Email}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new UserWithRolesViewModel
                {
                    User = user,
                    Roles = roles
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            // If the user is an Admin, ensure we do not delete the last administrator
            var isTargetAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isTargetAdmin)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    TempData["Error"] = "The last administrator cannot be deleted.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded
                    ? "User account deleted successfully."
                    : string.Join(" ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }
    }

    public class CreateUserViewModel
    {
        [Display(Name = "Full Name (Optional for Students)")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role selection is required.")]
        public string Role { get; set; } = "Student";
    }

    public class UserWithRolesViewModel
    {
        public IdentityUser User { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();
    }
}