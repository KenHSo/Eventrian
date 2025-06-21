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
        // TODO: Add demo users seeding here later
    }
}
