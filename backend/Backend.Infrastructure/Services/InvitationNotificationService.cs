using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Backend.Application.Services;

namespace Backend.Infrastructure.Services;

/// <summary>
/// In-memory implementation of invitation notification service using Channels
/// For production with multiple instances, consider using Redis Pub/Sub or SignalR
/// </summary>
public class InvitationNotificationService : IInvitationNotificationService
{
    // Dictionary of user ID to their notification channels
    private readonly ConcurrentDictionary<Guid, List<Channel<InvitationNotification>>> _userChannels = new();

    public async Task PublishInvitationCreatedAsync(
        Guid invitedUserId,
        InvitationNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (_userChannels.TryGetValue(invitedUserId, out var channels))
        {
            // Send to all active channels for this user
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

    public async IAsyncEnumerable<InvitationNotification> SubscribeToInvitationsAsync(
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Create a new channel for this subscription
        var channel = Channel.CreateUnbounded<InvitationNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Add channel to user's channels
        _userChannels.AddOrUpdate(
            userId,
            _ => new List<Channel<InvitationNotification>> { channel },
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
            // Cleanup: remove channel from user's channels
            if (_userChannels.TryGetValue(userId, out var channels))
            {
                lock (channels)
                {
                    channels.Remove(channel);

                    // If no more channels for this user, remove the entry
                    if (channels.Count == 0)
                    {
                        _userChannels.TryRemove(userId, out _);
                    }
                }
            }

            channel.Writer.Complete();
        }
    }
}
