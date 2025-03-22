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
    public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
    {
        private readonly DbContext _context;
        protected DbSet<T> _db => _context.Set<T>();
        public BaseRepository(DbContext context)
        {
            _context = context;


        }
        public Task<T> AddAsync(T entity)
        {
            _db.Add(entity);
            return Task.FromResult(entity);
        }
        public Task DeleteAsync(int id)
        {
            var entity = _db.Find(id);
            if (entity != null)
            {
                _db.Remove(entity);
            }
            return Task.CompletedTask;
        }
        public Task<IEnumerable<T>> GetAllAsync()
        {
            return Task.FromResult(_db.AsEnumerable());
        }
        public Task<T?> GetByIdAsync(int id)
        {
            return Task.FromResult(_db.Find(id));
        }
        public Task UpdateAsync(T entity)
        {
            _db.Update(entity);
            return Task.CompletedTask;
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


    }
}
