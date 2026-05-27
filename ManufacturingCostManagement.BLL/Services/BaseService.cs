using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingCostManagement.BLL.DTOs;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;
using ManufacturingCostManagement.DAL.Repositories;

namespace ManufacturingCostManagement.BLL.Services
{
    public abstract class BaseService<T> : IBaseService<T> where T : BaseEntity
    {
        protected readonly IRepository<T> _repository;

        protected BaseService(IRepository<T> repository)
        {
            _repository = repository;
        }

        public virtual async Task<T?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);

        public virtual async Task<IEnumerable<T>> GetAllAsync() => await _repository.GetAllAsync();

        public virtual async Task<T> CreateAsync(T entity)
        {
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T?> UpdateAsync(T entity)
        {
            var existing = await _repository.GetByIdAsync(entity.Id);
            if (existing == null) return null;
            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;
            await _repository.DeleteAsync(id);
            await _repository.SaveChangesAsync();
            return true;
        }

        public virtual async Task<int> CountAsync() => await _repository.CountAsync();

        public virtual async Task<PagedResult<T>> SearchAsync(string? keyword, int page, int pageSize)
        {
            var query = ApplySearch(_repository.Query(), keyword);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<T>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        protected abstract IQueryable<T> ApplySearch(IQueryable<T> query, string? keyword);
    }
}
