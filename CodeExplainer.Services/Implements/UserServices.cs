using CodeExplainer.BusinessObject;
using CodeExplainer.BusinessObject.Models;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.BusinessObject.Response;
using CodeExplainer.Repository.Interfaces;
using CodeExplainer.Services.Interfaces;
using CodeExplainer.Shared.Utils;

namespace CodeExplainer.Services.Implements;

public class UserServices : IUserServices
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly CloudinaryUploader _uploader;
    public UserServices(ApplicationDbContext context, IUserRepository userRepository, CloudinaryUploader uploader)
    {
        _context = context;
        _userRepository = userRepository;
        _uploader = uploader;
    }
    
    public async Task<UserUpdateProfileResponse?> UpdateUserAsync(UserProfileUpdateRequest request)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId.ToString());
        if (user != null)
        {
            if (!string.IsNullOrEmpty(request.PasswordHash))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordHash);
            }
            if (request.ProfilePictureUrl != null)
            {
                var uploadUrl = await _uploader.UploadImage(request.ProfilePictureUrl);
                if (!string.IsNullOrEmpty(uploadUrl))
                {
                    user.ProfilePictureUrl = uploadUrl;
                }
            }

            user.UserName = request.UserName;
            user.Email = request.Email;
            user.UpdatedAt = DateTime.Now;
            
            await _context.SaveChangesAsync();
            return new UserUpdateProfileResponse
            {
                UserName = user.UserName,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }
        return null;
    }
}