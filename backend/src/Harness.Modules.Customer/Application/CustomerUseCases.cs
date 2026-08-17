using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Customer.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Customer.Application;

public record RegisterCustomerCommand(string FullName, string Phone, string? Email) : IRequest<CustomerDto>;

public class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^0\d{9,10}$").WithMessage("Số điện thoại VN không hợp lệ.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, CustomerDto>
{
    private readonly IHarnessDbContext _db;

    public RegisterCustomerCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<CustomerDto> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.Set<Customer>().AnyAsync(c => c.Phone == request.Phone, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Số điện thoại {request.Phone} đã đăng ký.");

        var customer = Customer.Register(request.FullName, request.Phone, request.Email);
        _db.Set<Customer>().Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
        return new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email);
    }
}

public record CustomerDto(Guid Id, string FullName, string Phone, string? Email);

public record GetCustomerByPhoneQuery(string Phone) : IRequest<CustomerDto?>;

public class GetCustomerByPhoneQueryHandler : IRequestHandler<GetCustomerByPhoneQuery, CustomerDto?>
{
    private readonly IHarnessDbContext _db;

    public GetCustomerByPhoneQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<CustomerDto?> Handle(GetCustomerByPhoneQuery request, CancellationToken cancellationToken)
    {
        var customer = await _db.Set<Customer>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Phone == request.Phone, cancellationToken);
        return customer is null ? null : new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email);
    }
}
