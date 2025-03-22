using ProjectName.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectName.Domain.Interfaces
{
    public interface IUserRepository: IBaseRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);

    }
}
