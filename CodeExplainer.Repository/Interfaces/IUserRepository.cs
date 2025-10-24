using CodeExplainer.BusinessObject.Enum;
using CodeExplainer.BusinessObject.Models;

namespace CodeExplainer.Repository.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(string userId);
    Task<User?> FindByNameAsync(string userName);
    Task CreateAsync(User user);
    Task CreateAsync(User user, string password);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task<User?> AddToRoleAsync(User user, UserRole roleName);
    Task<User?> ConfirmEmailAsync(User user);
    Task<bool> CheckPasswordAsync(User user, string password);
    Task<bool> IsEmailConfirmedAsync(User user);
    Task<User?> ResetPasswordAsync(User user, string newPassword);
}