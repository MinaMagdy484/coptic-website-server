using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using static System.Formats.Asn1.AsnWriter;
using DEPI_Project1.Models;

namespace DEPI_Project1.Services
{
    public class DatabaseInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseInitializer(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Create roles
                await CreateRolesAsync();

                // Create admin user
                await CreateAdminUserAsync();

                Console.WriteLine("✅ Database initialized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error initializing database: {ex.Message}");
                throw;
            }
        }

        private async Task CreateRolesAsync()
        {
            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"✅ Role '{roleName}' created");
                }
            }
        }

        private async Task CreateAdminUserAsync()
        {
            const string adminEmail = "fr.arsany@gmail.com";
            const string adminPassword = "Fr.arsany123#";

            // Check if admin user already exists
            var existingUser = await _userManager.FindByEmailAsync(adminEmail);
            if (existingUser != null)
            {
                Console.WriteLine($"⚠️  Admin user already exists: {adminEmail}");
                return;
            }

            // Create admin user
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                // Add user to Admin role
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                
                Console.WriteLine($"✅ Admin user created successfully!");
                Console.WriteLine($"Email: {adminEmail}");
                Console.WriteLine($"Role: Admin");
            }
            else
            {
                Console.WriteLine($"❌ Error creating admin user:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - {error.Description}");
                }
            }
        }
    }
}
