using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingCostManagement.BLL.DTOs;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.BLL.Interfaces
{
    public interface IBaseService<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<PagedResult<T>> SearchAsync(string? keyword, int page, int pageSize);
        Task<T> CreateAsync(T entity);
        Task<T?> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        Task<int> CountAsync();
    }
}
