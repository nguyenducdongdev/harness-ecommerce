using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Customer.Domain;

public enum ChatSessionStatus
{
    Active = 1,
    Assigned = 2,
    Closed = 3
}

public enum ChatMessageSender
{
    Customer = 1,
    Agent = 2,
    System = 3
}

public class ChatSession : AuditableEntity<Guid>
{
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public Guid? CustomerId { get; private set; }
    public ChatSessionStatus Status { get; private set; } = ChatSessionStatus.Active;
    public Guid? AssignedAgentId { get; private set; }
    public string? AssignedAgentName { get; private set; }
    public DateTime LastActivityAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; private set; }

    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    private ChatSession() { }

    public static ChatSession Create(string customerName, string customerPhone, Guid? customerId = null)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Khách hàng" : customerName.Trim(),
            CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? "" : customerPhone.Trim(),
            CustomerId = customerId,
            Status = ChatSessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        session.AddMessage(
            ChatMessageSender.System,
            "Hệ Thống",
            $"Xin chào {session.CustomerName}! Cảm ơn bạn đã liên hệ Nội Thất Harness. Chuyên viên tư vấn sẽ phản hồi bạn trong giây lát.",
            null
        );

        return session;
    }

    public ChatMessage AddMessage(ChatMessageSender senderType, string senderName, string messageText, string? senderId = null)
    {
        if (Status == ChatSessionStatus.Closed && senderType != ChatMessageSender.System)
        {
            Status = ChatSessionStatus.Active;
        }

        var msg = ChatMessage.Create(Id, senderType, senderName, messageText, senderId);
        _messages.Add(msg);
        LastActivityAt = DateTime.UtcNow;
        return msg;
    }

    public void AssignAgent(Guid agentId, string agentName)
    {
        AssignedAgentId = agentId;
        AssignedAgentName = agentName;
        Status = ChatSessionStatus.Assigned;
        LastActivityAt = DateTime.UtcNow;

        AddMessage(
            ChatMessageSender.System,
            "Hệ Thống",
            $"Chuyên viên tư vấn {agentName} đã tham gia cuộc hội thoại.",
            agentId.ToString()
        );
    }

    public void CloseSession()
    {
        Status = ChatSessionStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;

        var msg = ChatMessage.Create(
            Id,
            ChatMessageSender.System,
            "Hệ Thống",
            "Phiên tư vấn đã kết thúc. Cảm ơn bạn đã tin tưởng Nội Thất Harness!",
            null
        );
        _messages.Add(msg);
    }

}

public class ChatMessage : Entity<Guid>
{
    public Guid ChatSessionId { get; private set; }
    public ChatMessageSender SenderType { get; private set; }
    public string? SenderId { get; private set; }
    public string SenderName { get; private set; } = string.Empty;
    public string MessageText { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; } = DateTime.UtcNow;
    public bool IsRead { get; private set; }

    private ChatMessage() { }

    public static ChatMessage Create(Guid chatSessionId, ChatMessageSender senderType, string senderName, string messageText, string? senderId = null)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = chatSessionId,
            SenderType = senderType,
            SenderId = senderId,
            SenderName = string.IsNullOrWhiteSpace(senderName) ? "Vô danh" : senderName,
            MessageText = messageText ?? string.Empty,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}

