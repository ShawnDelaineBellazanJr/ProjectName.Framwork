using Microsoft.EntityFrameworkCore;
using ProjectName.Domain.Entities;
using ProjectName.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectName.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(DbContext context) : base(context)
        {
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _db.FirstOrDefaultAsync(x => x.Username == username);
        }
    }
}
