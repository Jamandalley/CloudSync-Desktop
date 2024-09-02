using Google.Apis.Drive.v3;
using System.Threading.Tasks;

namespace CloudSyncV2.Services
{
    public interface IAuthenticationService
    {
        Task<bool> AuthenticateUserAsync(string email);
        Task<bool> IsUserAuthenticatedAsync(string email);
        Task UpdateAuthenticationStatusAsync(string email, bool isAuthenticated);
    }
}