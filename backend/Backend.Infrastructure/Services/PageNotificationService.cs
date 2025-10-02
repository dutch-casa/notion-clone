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

    public async Task PublishPageNotificationAsync(
        Guid orgId,
        PageNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (_orgChannels.TryGetValue(orgId, out var channels))
        {
            // Send to all active channels for this organization
            var tasks = channels.Select(async channel =>
            {
                try
                {
                    await channel.Writer.WriteAsync(notification, cancellationToken);
                }
                catch (ChannelClosedException)
                {
                    // Channel was closed, will be cleaned up by subscriber
                }
            });

            await Task.WhenAll(tasks);
        }
    }

    public async IAsyncEnumerable<PageNotification> SubscribeToPageNotificationsAsync(
        Guid orgId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
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

        try
        {
            // Read from channel until cancelled
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return notification;
            }
        }
        finally
        {
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
