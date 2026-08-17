using System.Text.Json;
using Harness.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace Harness.BuildingBlocks.Infrastructure.Persistence;

public static class OutboxExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Ghi integration event vào outbox — gọi TRƯỚC SaveChangesAsync, cùng transaction.</summary>
    public static void AddToOutbox(this IHarnessDbContext context, IntegrationEvent integrationEvent)
    {
        context.Set<OutboxMessage>().Add(new OutboxMessage
        {
            EventType = integrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions),
            OccurredAt = integrationEvent.OccurredAt
        });
    }
}
