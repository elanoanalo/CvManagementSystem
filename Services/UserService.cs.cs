using CvManagementSystem.Data;
using CvManagementSystem.Entities;
using Microsoft.AspNetCore.Identity;

namespace CvManagementSystem.Services
{
    public class UserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly AppDbContext _context;

        public UserService(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task ChangeUserRoleAsync(User user, UserRole newRole)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var newRoleName = newRole.ToString();
            if (!await _roleManager.RoleExistsAsync(newRoleName))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(newRoleName));

            await _userManager.AddToRoleAsync(user, newRoleName);

            user.Role = newRole;
            await _context.SaveChangesAsync();
        }
    }
}