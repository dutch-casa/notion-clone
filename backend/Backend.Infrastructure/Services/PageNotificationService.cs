using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Backend.Application.Services;

namespace Backend.Infrastructure.Services;

/// <summary>
/// In-memory implementation of page notification service using Channels.
/// For production with multiple instances, consider using Redis Pub/Sub or SignalR.
/// </summary>
public class PageNotificationService : IPageNotificationService
{
    // Dictionary of org ID to their notification channels
    private readonly ConcurrentDictionary<Guid, List<Channel<PageNotification>>> _orgChannels = new();

    private readonly Microsoft.Extensions.Logging.ILogger<PageNotificationService> _logger;

    public PageNotificationService(Microsoft.Extensions.Logging.ILogger<PageNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task PublishPageNotificationAsync(
        Guid orgId,
        PageNotification notification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing {EventType} notification for org {OrgId}, Active subscribers: {Count}",
            notification.EventType, orgId, _orgChannels.TryGetValue(orgId, out var ch) ? ch.Count : 0);

        if (_orgChannels.TryGetValue(orgId, out var channels))
        {
            // Send to all active channels for this organization
            var tasks = channels.Select(async channel =>
            {
                try
                {
                    await channel.Writer.WriteAsync(notification, cancellationToken);
                    _logger.LogDebug("Sent {EventType} notification to channel", notification.EventType);
                }
                catch (ChannelClosedException)
                {
                    _logger.LogWarning("Channel closed for org {OrgId}", orgId);
                    // Channel was closed, will be cleaned up by subscriber
                }
            });

            await Task.WhenAll(tasks);
        }
        else
        {
            _logger.LogWarning("No subscribers for org {OrgId}", orgId);
        }
    }

    public async IAsyncEnumerable<PageNotification> SubscribeToPageNotificationsAsync(
        Guid orgId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("New SSE subscription for org {OrgId}", orgId);

        // Create a new channel for this subscription
        var channel = Channel.CreateUnbounded<PageNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Add channel to org's channels
        _orgChannels.AddOrUpdate(
            orgId,
            _ => new List<Channel<PageNotification>> { channel },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(channel);
                }
                return existing;
            });

        _logger.LogInformation("SSE subscription added. Total subscribers for org {OrgId}: {Count}",
            orgId, _orgChannels[orgId].Count);

        try
        {
            // Read from channel until cancelled
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                _logger.LogDebug("Yielding {EventType} notification to SSE client", notification.EventType);
                yield return notification;
            }
        }
        finally
        {
            _logger.LogInformation("SSE subscription ended for org {OrgId}", orgId);

            // Cleanup: remove channel from org's channels
            if (_orgChannels.TryGetValue(orgId, out var channels))
            {
                lock (channels)
                {
                    channels.Remove(channel);

                    // If no more channels for this org, remove the entry
                    if (channels.Count == 0)
                    {
                        _orgChannels.TryRemove(orgId, out _);
                    }
                }
            }

            channel.Writer.Complete();
        }
    }
}
