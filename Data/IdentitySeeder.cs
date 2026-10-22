using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentCourseManagement.Models;

namespace StudentCourseManagement.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAdminAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            using var scope = services.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            // 1. Seed Roles: Admin, Faculty, Student
            var roles = new[] { "Admin", "Faculty", "Student" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var roleResult = await roleManager.CreateAsync(
                        new IdentityRole(roleName));

                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join("; ",
                                roleResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            // 2. Default Accounts to seed
            var defaultAccounts = new[]
            {
                new { Email = configuration["AdminSeed:Email"] ?? "admin@student.com", Password = configuration["AdminSeed:Password"] ?? "Admin@123", Role = "Admin" },
                new { Email = "faculty@student.com", Password = "Faculty@123", Role = "Faculty" },
                new { Email = "student@student.com", Password = "Student@123", Role = "Student" }
            };

            foreach (var acc in defaultAccounts)
            {
                var user = await userManager.FindByEmailAsync(acc.Email);

                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = acc.Email,
                        Email = acc.Email,
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(user, acc.Password);

                    if (!createResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Failed creating {acc.Role} user: " +
                            string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    }
                }

                if (!await userManager.IsInRoleAsync(user, acc.Role))
                {
                    var addRoleResult = await userManager.AddToRoleAsync(user, acc.Role);

                    if (!addRoleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Failed assigning role {acc.Role}: " +
                            string.Join("; ", addRoleResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            // 3. Ensure student@student.com exists in the Students table
            var studentEmail = "student@student.com";
            var existingStudent = await dbContext.Students
                .FirstOrDefaultAsync(s => s.Email == studentEmail);

            if (existingStudent == null)
            {
                dbContext.Students.Add(new Student
                {
                    FullName = "Alex Johnson",
                    Email = studentEmail,
                    Phone = "+91 98765 00001",
                    Department = "Computer Science"
                });

                await dbContext.SaveChangesAsync();
            }
        }
    }
}