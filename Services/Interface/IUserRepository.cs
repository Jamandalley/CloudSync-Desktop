
using CloudSyncV2.Model;

namespace CloudSyncV2.Services;

public interface IUserRepository
{
    Task<UserModel> GetUserByEmailAsync(string email);
    Task AddUserAsync(UserModel user);
    Task UpdateUserAsync(UserModel user);
}