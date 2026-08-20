using Harness.Modules.Customer.Domain;
using Xunit;

namespace Harness.UnitTests;

public class SupportChatTests
{
    [Fact]
    public void Create_ChatSession_CreatesInitialBotGreetingMessage()
    {
        var session = ChatSession.Create("Nguyễn Văn A", "0901234567");

        Assert.NotNull(session);
        Assert.Equal("Nguyễn Văn A", session.CustomerName);
        Assert.Equal("0901234567", session.CustomerPhone);
        Assert.Equal(ChatSessionStatus.Active, session.Status);
        Assert.Single(session.Messages);

        var initialMsg = session.Messages.First();
        Assert.Equal(ChatMessageSender.System, initialMsg.SenderType);
        Assert.Contains("Xin chào Nguyễn Văn A", initialMsg.MessageText);
    }

    [Fact]
    public void AddMessage_AddsCustomerAndAgentMessagesCorrectly()
    {
        var session = ChatSession.Create("Trần Thị B", "0987654321");

        session.AddMessage(ChatMessageSender.Customer, "Trần Thị B", "Cho mình hỏi bộ sofa góc này có miễn phí vận chuyển không?");
        Assert.Equal(2, session.Messages.Count);

        var agentId = Guid.NewGuid();
        session.AssignAgent(agentId, "Tư Vấn Viên Tuấn");

        Assert.Equal(ChatSessionStatus.Assigned, session.Status);
        Assert.Equal(agentId, session.AssignedAgentId);
        Assert.Equal("Tư Vấn Viên Tuấn", session.AssignedAgentName);

        session.AddMessage(ChatMessageSender.Agent, "Tư Vấn Viên Tuấn", "Dạ chào chị B, đơn hàng sofa bên em đang có ưu đãi miễn phí giao hàng nội thành ạ!", agentId.ToString());

        Assert.Equal(4, session.Messages.Count); // initial + customer + assign system msg + agent msg
    }

    [Fact]
    public void CloseSession_UpdatesStatusAndAddsSystemMessage()
    {
        var session = ChatSession.Create("Lê Văn C", "0911223344");
        session.CloseSession();

        Assert.Equal(ChatSessionStatus.Closed, session.Status);
        Assert.NotNull(session.ClosedAt);
        Assert.EndsWith("Phiên tư vấn đã kết thúc. Cảm ơn bạn đã tin tưởng Nội Thất Harness!", session.Messages.Last().MessageText);
    }

    [Fact]
    public void AddMessage_OnClosedSession_ReopensSessionToActive()
    {
        var session = ChatSession.Create("Lê Văn C", "0911223344");
        session.CloseSession();
        Assert.Equal(ChatSessionStatus.Closed, session.Status);

        session.AddMessage(ChatMessageSender.Customer, "Lê Văn C", "Em muốn hỏi thêm về thời gian bảo hành.");
        Assert.Equal(ChatSessionStatus.Active, session.Status);
    }
}
