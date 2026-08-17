using FluentValidation;
using Harness.BuildingBlocks.Application;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Customer.Domain;
using Harness.Modules.Customer.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Harness.Modules.Customer.Application;

public record RequestOtpCommand(string Phone) : IRequest<OtpRequestDto>;

public class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
        => RuleFor(x => x.Phone).NotEmpty().Matches(@"^0\d{9,10}$").WithMessage("Số điện thoại VN không hợp lệ.");
}

public record OtpRequestDto(string Phone, string? OtpCode, int ExpiryMinutes);

public class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand, OtpRequestDto>
{
    private readonly OtpService _otp;
    private readonly IOptions<OtpOptions> _options;

    public RequestOtpCommandHandler(OtpService otp, IOptions<OtpOptions> options)
    {
        _otp = otp;
        _options = options;
    }

    public async Task<OtpRequestDto> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        var code = await _otp.GenerateAsync(request.Phone, cancellationToken);
        var showCode = _options.Value.ReturnCodeInResponse ? code : null;
        return new OtpRequestDto(request.Phone, showCode, _options.Value.ExpiryMinutes);
    }
}

public record VerifyOtpCommand(string Phone, string Code, string? Name = null) : IRequest<OtpSessionDto>;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^0\d{9,10}$");
        RuleFor(x => x.Code).NotEmpty().Length(4, 8);
    }
}

public record OtpSessionDto(string AccessToken, string Phone, Guid CustomerId, bool IsNewCustomer);

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, OtpSessionDto>
{
    private readonly IHarnessDbContext _db;
    private readonly OtpService _otp;

    public VerifyOtpCommandHandler(IHarnessDbContext db, OtpService otp)
    {
        _db = db;
        _otp = otp;
    }

    public async Task<OtpSessionDto> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var ok = await _otp.VerifyAsync(request.Phone, request.Code, cancellationToken);
        if (!ok)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    nameof(VerifyOtpCommand.Code), "Mã OTP không đúng hoặc đã hết hạn.")
            });
        }

        var customer = await _db.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Phone == request.Phone, cancellationToken);
        var isNew = customer is null;

        if (customer is null)
        {
            customer = Customer.Register(string.IsNullOrWhiteSpace(request.Name) ? request.Phone : request.Name!, request.Phone);
            _db.Set<Customer>().Add(customer);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var token = await _otp.IssueSessionAsync(request.Phone, cancellationToken);
        return new OtpSessionDto(token, request.Phone, customer.Id, isNew);
    }
}
