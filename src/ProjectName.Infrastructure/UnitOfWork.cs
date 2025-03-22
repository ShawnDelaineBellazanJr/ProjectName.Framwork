using Microsoft.EntityFrameworkCore;
using ProjectName.AIServices;
using ProjectName.Domain.Interfaces;
using ProjectName.Infrastructure.Repositories;


namespace ProjectName.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            Users = new UserRepository(_context);
        }
        public IUserRepository Users { get; private set; }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
