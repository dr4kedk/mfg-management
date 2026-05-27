using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;
using ManufacturingCostManagement.DAL.Repositories;

namespace ManufacturingCostManagement.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepo;

        public AuthService(IRepository<User> userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _userRepo.Query()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null) return null;
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
            return user;
        }

        public async Task<User> RegisterAsync(User user, string password, int roleId)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.RoleId = roleId;
            user.IsActive = true;
            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();
            return user;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash)) return false;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepo.UpdateAsync(user);
            await _userRepo.SaveChangesAsync();
            return true;
        }
    }
}
