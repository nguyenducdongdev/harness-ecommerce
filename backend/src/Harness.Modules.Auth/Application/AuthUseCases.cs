using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Auth.Domain;
using Harness.Modules.Auth.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Auth.Application;

public record AdminLoginCommand(string Username, string Password) : IRequest<AdminLoginResponseDto>;

public class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public record AdminLoginResponseDto(
    string AccessToken, DateTimeOffset ExpiresAt,
    Guid AdminId, string Username, string DisplayName, IReadOnlyList<string> Roles);

public class AdminLoginCommandHandler : IRequestHandler<AdminLoginCommand, AdminLoginResponseDto>
{
    private readonly IHarnessDbContext _db;
    private readonly JwtTokenService _jwt;

    public AdminLoginCommandHandler(IHarnessDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AdminLoginResponseDto> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var user = await _db.Set<AdminUser>().AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("Sai tài khoản hoặc mật khẩu.");

        if (!PasswordHashHelper.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Sai tài khoản hoặc mật khẩu.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToArray();
        if (roles.Length == 0)
            throw new UnauthorizedAccessException("Tài khoản chưa được gán vai trò.");

        // Cập nhật LastLoginAt (tách truy vấn — tránh tracking trùng user đã AsNoTracking)
        var tracked = await _db.Set<AdminUser>()
            .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
        if (tracked is not null)
        {
            tracked.MarkLoggedIn();
            await _db.SaveChangesAsync(cancellationToken);
        }

        var token = _jwt.CreateToken(user, roles);
        return new AdminLoginResponseDto(token.Token, token.ExpiresAt,
            user.Id, user.Username, user.DisplayName, roles);
    }
}

public record GetCurrentAdminQuery(Guid AdminId) : IRequest<AdminProfileDto>;

public record AdminProfileDto(Guid Id, string Username, string DisplayName, IReadOnlyList<string> Roles);

public class GetCurrentAdminQueryHandler : IRequestHandler<GetCurrentAdminQuery, AdminProfileDto>
{
    private readonly IHarnessDbContext _db;

    public GetCurrentAdminQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<AdminProfileDto> Handle(GetCurrentAdminQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Set<AdminUser>().AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.AdminId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản admin.");

        return new AdminProfileDto(user.Id, user.Username, user.DisplayName,
            user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToArray());
    }
}

public record ChangePasswordCommand(Guid AdminId, string OldPassword, string NewPassword) : IRequest<bool>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .WithMessage("Mật khẩu mới tối thiểu 8 ký tự.");
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IHarnessDbContext _db;

    public ChangePasswordCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Set<AdminUser>()
            .FirstOrDefaultAsync(u => u.Id == request.AdminId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản admin.");

        if (!PasswordHashHelper.Verify(request.OldPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Mật khẩu cũ không đúng.");

        user.SetPassword(PasswordHashHelper.Hash(request.NewPassword));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}