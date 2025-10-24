using CodeExplainer.BusinessObject.Models;
using CodeExplainer.BusinessObject.Request;
using CodeExplainer.BusinessObject.Response;

namespace CodeExplainer.Services.Interfaces;

public interface IUserServices
{
    Task<UserUpdateProfileResponse?> UpdateUserAsync(UserProfileUpdateRequest request)
}