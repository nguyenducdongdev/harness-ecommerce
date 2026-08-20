using Harness.Modules.Customer.Domain;

namespace Harness.Modules.Customer.Application;

public record ChatMessageDto(
    Guid Id,
    Guid ChatSessionId,
    string SenderType,
    string? SenderId,
    string SenderName,
    string MessageText,
    DateTime SentAt,
    bool IsRead
);

public record ChatSessionDto(
    Guid Id,
    string CustomerName,
    string CustomerPhone,
    Guid? CustomerId,
    string Status,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    DateTime? ClosedAt,
    List<ChatMessageDto> Messages
);

public static class ChatMappingExtensions
{
    public static ChatSessionDto ToDto(this ChatSession session)
    {
        return new ChatSessionDto(
            session.Id,
            session.CustomerName,
            session.CustomerPhone,
            session.CustomerId,
            session.Status.ToString(),
            session.AssignedAgentId,
            session.AssignedAgentName,
            session.CreatedAt.UtcDateTime,
            session.LastActivityAt,
            session.ClosedAt,
            session.Messages.OrderBy(m => m.SentAt).Select(m => m.ToDto()).ToList()
        );
    }

    public static ChatMessageDto ToDto(this ChatMessage msg)
    {
        return new ChatMessageDto(
            msg.Id,
            msg.ChatSessionId,
            msg.SenderType.ToString(),
            msg.SenderId,
            msg.SenderName,
            msg.MessageText,
            msg.SentAt,
            msg.IsRead
        );
    }
}

