using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingCostManagement.DAL.Data;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.Web.Authorization
{
    public interface IPermissionService
    {
        Task<HashSet<string>> GetCodesForRoleAsync(int roleId);
        Task<List<Permission>> GetAllAsync();
        Task<List<Role>> GetAllRolesAsync();
        Task<HashSet<(int RoleId, int PermissionId)>> GetMatrixAsync();
        Task SetForRoleAsync(int roleId, IEnumerable<int> permissionIds);
    }

    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _db;
        public PermissionService(AppDbContext db) { _db = db; }

        public async Task<HashSet<string>> GetCodesForRoleAsync(int roleId)
        {
            return (await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission!.Code)
                .ToListAsync()).ToHashSet();
        }

        public async Task<List<Permission>> GetAllAsync()
            => await _db.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Action).ToListAsync();

        public async Task<List<Role>> GetAllRolesAsync()
            => await _db.Roles.OrderBy(r => r.Id).ToListAsync();

        public async Task<HashSet<(int RoleId, int PermissionId)>> GetMatrixAsync()
        {
            var rows = await _db.RolePermissions
                .Select(rp => new { rp.RoleId, rp.PermissionId })
                .ToListAsync();
            return rows.Select(r => (r.RoleId, r.PermissionId)).ToHashSet();
        }

        public async Task SetForRoleAsync(int roleId, IEnumerable<int> permissionIds)
        {
            var newSet = permissionIds.ToHashSet();
            var existing = await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            // remove rows no longer wanted
            foreach (var rp in existing)
                if (!newSet.Contains(rp.PermissionId)) _db.RolePermissions.Remove(rp);

            // add new rows
            var existingIds = existing.Select(rp => rp.PermissionId).ToHashSet();
            foreach (var pid in newSet)
                if (!existingIds.Contains(pid))
                    _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pid });

            await _db.SaveChangesAsync();
        }
    }
}
