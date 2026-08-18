using Harness.Modules.Auth.Domain;
using Microsoft.AspNetCore.Identity;

namespace Harness.Modules.Auth.Infrastructure;

/// <summary>
/// Bọc PasswordHasher của ASP.NET Identity để seed & login admin dùng chung cách hash.
/// </summary>
public static class PasswordHashHelper
{
    private static readonly PasswordHasher<AdminUser> Hasher = new();

    public static string Hash(string plainPassword)
        => Hasher.HashPassword(null!, plainPassword);

    public static bool Verify(string plainPassword, string storedHash)
        => Hasher.VerifyHashedPassword(null!, storedHash, plainPassword) != PasswordVerificationResult.Failed;
}