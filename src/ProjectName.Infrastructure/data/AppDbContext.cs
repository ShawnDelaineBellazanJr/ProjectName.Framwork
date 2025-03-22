using Microsoft.EntityFrameworkCore;
using ProjectName.Domain.Entities;

namespace ProjectName.AIServices
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; } = default!;
    }
}
