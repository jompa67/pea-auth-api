using System.Net;
using AuthApi.Contracts.Login;
using AuthApi.Contracts.Register;
using AuthApi.Models;

namespace AuthApi.Services.Interface;

public interface ILoginService
{
    Task<RegisterResult> RegisterWithPassword(RegisterWithPasswordRequest dto, CancellationToken cancellationToken);
    Task<LoginResponse> LoginWithPassword(LoginRequest dto, CancellationToken cancellationToken);

    Task<(LoginResponse Response, HttpStatusCode StatusCode, string Message)> GetRefreshToken(string token,
        string oldRefreshToken, CancellationToken cancellationToken);

    Task<bool> LogoutUser(string username, CancellationToken cancellationToken);
    
    Task<bool> CreateAdminUser(string username, CancellationToken cancellationToken);
    Task<bool> ExistsUser(string userName, CancellationToken cancellationToken);
    Task<bool> IsUserAdmin(string userName, CancellationToken cancellationToken);
    public Task<UserProfile?> GetUserProfile(string userName, CancellationToken cancellationToken);

}