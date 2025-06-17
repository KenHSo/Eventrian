using Microsoft.EntityFrameworkCore;

namespace Eventrian.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets
    // public DbSet<Event> Events { get; set; }
}
