using CertGuard.Core.Models;

namespace CertGuard.Core.DTOs;

public record LoginResponse(string Token, User User);
