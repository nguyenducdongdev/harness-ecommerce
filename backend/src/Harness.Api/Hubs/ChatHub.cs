using Harness.Modules.Customer.Application;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Harness.Api.Hubs;

public class ChatHub : Hub
{
    private readonly IMediator _mediator;

    public ChatHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task JoinSessionGroup(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
    }

    public async Task JoinSupportGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "support_agents");
    }

    public async Task SendMessage(string sessionId, string senderType, string senderName, string messageText, string? senderId = null)
    {
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
            return;

        var command = new SendChatMessageCommand(parsedSessionId, senderType, senderName, messageText, senderId);
        var messageDto = await _mediator.Send(command);

        // Broadcast to everyone in this session group
        await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", messageDto);

        // Notify support agents of new message/activity
        await Clients.Group("support_agents").SendAsync("SessionActivity", new { sessionId, lastMessage = messageText, sentAt = messageDto.SentAt });
    }
}
