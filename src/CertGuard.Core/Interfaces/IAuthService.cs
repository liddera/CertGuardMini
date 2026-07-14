using CertGuard.Core.DTOs;
using CertGuard.Core.Models;

namespace CertGuard.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<User> GetMeAsync();
}
