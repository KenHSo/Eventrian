using Eventrian.Api.Features.Auth.Models;
using Microsoft.AspNetCore.Identity;

namespace Eventrian.Api.Data;

public class IdentitySeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public IdentitySeeder(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await CreateRoleAsync();

        // TODO: Make better demo data for production use
        await CreateDemoUserAsync("1@1", "1", "Admin");
        await CreateDemoUserAsync("2@2", "2", "Customer");
        await CreateDemoUserAsync("3@3", "3", "Organizer");
    }

    private async Task CreateRoleAsync()
    {
        string[] roles = { "Admin", "Customer", "Organizer" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                Console.WriteLine($"Seeding Role: {role}");

                var result = await _roleManager.CreateAsync(new IdentityRole(role));

                if (!result.Succeeded)
                    throw new Exception($"Failed to create role {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            }
        }
    }

    private async Task CreateDemoUserAsync(string email, string password, string role)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            Console.WriteLine($"Seeding User {email}");

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Demo",
                LastName = role == "Admin" ? "Admin" : role == "Customer" ? "Customer" : "Organizer"
            };

            var result = await _userManager.CreateAsync(newUser, password);

            if (result.Succeeded)
                await _userManager.AddToRoleAsync(newUser, role);
            else
                throw new Exception($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        }
    }
}
