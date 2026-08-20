using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Customer.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Customer.Application;

public record StartChatSessionCommand(
    string CustomerName,
    string CustomerPhone,
    Guid? CustomerId = null
) : IRequest<ChatSessionDto>;

public record SendChatMessageCommand(
    Guid ChatSessionId,
    string SenderType,
    string SenderName,
    string MessageText,
    string? SenderId = null
) : IRequest<ChatMessageDto>;

public record GetChatMessagesQuery(
    Guid ChatSessionId
) : IRequest<List<ChatMessageDto>>;

public record GetChatSessionsQuery(
    string? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<List<ChatSessionDto>>;

public record AssignChatSessionCommand(
    Guid ChatSessionId,
    Guid AgentId,
    string AgentName
) : IRequest<ChatSessionDto>;

public record CloseChatSessionCommand(
    Guid ChatSessionId
) : IRequest<ChatSessionDto>;


public class StartChatSessionCommandHandler : IRequestHandler<StartChatSessionCommand, ChatSessionDto>
{
    private readonly IHarnessDbContext _db;

    public StartChatSessionCommandHandler(IHarnessDbContext db)
    {
        _db = db;
    }

    public async Task<ChatSessionDto> Handle(StartChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = ChatSession.Create(request.CustomerName, request.CustomerPhone, request.CustomerId);
        _db.Set<ChatSession>().Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return session.ToDto();
    }
}

public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, ChatMessageDto>
{
    private readonly IHarnessDbContext _db;

    public SendChatMessageCommandHandler(IHarnessDbContext db)
    {
        _db = db;
    }

    public async Task<ChatMessageDto> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.Set<ChatSession>()
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == request.ChatSessionId, cancellationToken);

        if (session == null)
            throw new KeyNotFoundException($"Không tìm thấy phiên chat {request.ChatSessionId}");

        if (!Enum.TryParse<ChatMessageSender>(request.SenderType, true, out var senderType))
            senderType = ChatMessageSender.Customer;

        var message = session.AddMessage(senderType, request.SenderName, request.MessageText, request.SenderId);
        await _db.SaveChangesAsync(cancellationToken);

        return message.ToDto();
    }
}

public class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, List<ChatMessageDto>>
{
    private readonly IHarnessDbContext _db;

    public GetChatMessagesQueryHandler(IHarnessDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChatMessageDto>> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _db.Set<ChatMessage>()
            .AsNoTracking()
            .Where(m => m.ChatSessionId == request.ChatSessionId)
            .OrderBy(m => m.SentAt)
            .ToListAsync(cancellationToken);

        return messages.Select(m => m.ToDto()).ToList();
    }
}

public class GetChatSessionsQueryHandler : IRequestHandler<GetChatSessionsQuery, List<ChatSessionDto>>
{
    private readonly IHarnessDbContext _db;

    public GetChatSessionsQueryHandler(IHarnessDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChatSessionDto>> Handle(GetChatSessionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Set<ChatSession>()
            .Include(s => s.Messages)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ChatSessionStatus>(request.Status, true, out var status))
        {
            query = query.Where(s => s.Status == status);
        }

        var sessions = await query
            .OrderByDescending(s => s.LastActivityAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return sessions.Select(s => s.ToDto()).ToList();
    }
}

public class AssignChatSessionCommandHandler : IRequestHandler<AssignChatSessionCommand, ChatSessionDto>
{
    private readonly IHarnessDbContext _db;

    public AssignChatSessionCommandHandler(IHarnessDbContext db)
    {
        _db = db;
    }

    public async Task<ChatSessionDto> Handle(AssignChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.Set<ChatSession>()
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == request.ChatSessionId, cancellationToken);

        if (session == null)
            throw new KeyNotFoundException($"Không tìm thấy phiên chat {request.ChatSessionId}");

        session.AssignAgent(request.AgentId, request.AgentName);
        await _db.SaveChangesAsync(cancellationToken);

        return session.ToDto();
    }
}

public class CloseChatSessionCommandHandler : IRequestHandler<CloseChatSessionCommand, ChatSessionDto>
{
    private readonly IHarnessDbContext _db;

    public CloseChatSessionCommandHandler(IHarnessDbContext db)
    {
        _db = db;
    }

    public async Task<ChatSessionDto> Handle(CloseChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.Set<ChatSession>()
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == request.ChatSessionId, cancellationToken);

        if (session == null)
            throw new KeyNotFoundException($"Không tìm thấy phiên chat {request.ChatSessionId}");

        session.CloseSession();
        await _db.SaveChangesAsync(cancellationToken);

        return session.ToDto();
    }
}

