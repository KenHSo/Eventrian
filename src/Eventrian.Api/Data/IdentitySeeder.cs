using Eventrian.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Eventrian.Api.Data;

public class IdentitySeeder
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentitySeeder(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        // TODO: Make better demo data for production use
        await CreateDemoUserAsync("1@1", "1", "Admin");
        await CreateDemoUserAsync("2@2", "2", "Customer");
    }

    private async Task CreateDemoUserAsync(string email, string password, string role)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            Console.WriteLine($"Seeding Demo {role}");

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Demo",
                LastName = role == "Admin" ? "Admin" : "Customer"
            };

            var result = await _userManager.CreateAsync(newUser, password);

            if (result.Succeeded)
                await _userManager.AddToRoleAsync(newUser, role);
            else
                throw new Exception($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        }
    }
}
