using Backend.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Backend.Presentation.Hubs;

/// <summary>
/// SignalR hub for real-time document collaboration.
/// Manages document sessions, broadcasts updates, and handles presence awareness.
/// </summary>
/// <remarks>
/// SCALABILITY LIMITATION: This hub uses static in-memory dictionaries for tracking
/// page connections and user presence. While SignalR itself is configured with Redis
/// backplane for message distribution across multiple servers (see Program.cs:57-61),
/// the static state (_pageConnections and _userPresence) will NOT be shared between
/// server instances in a scale-out scenario.
///
/// For production multi-server deployments, consider:
/// 1. Moving connection tracking to Redis with pub/sub for presence events
/// 2. Using SignalR Groups (already implemented) which work with Redis backplane
/// 3. Storing user presence in Redis Hash with connection ID as key
/// 4. Using Redis Sets for tracking which connections are in each page
///
/// Current implementation is suitable for:
/// - Single-server deployments
/// - Development environments
/// - Proof-of-concept demonstrations
///
/// The CRDT state persistence (lines 106-122, 324-348) correctly uses IDistributedCache
/// (Redis) and will work correctly in multi-server scenarios.
/// </remarks>
[Authorize]
public class DocumentHub : Hub
{
    // WARNING: Static state - see class remarks about scalability limitations
    private static readonly Dictionary<string, HashSet<string>> _pageConnections = new();
    private static readonly Dictionary<string, UserPresence> _userPresence = new();
    private static readonly object _lock = new();
    private readonly IDistributedCache _cache;

    public DocumentHub(IDistributedCache cache)
    {
        _cache = cache;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value
                    ?? Context.User?.FindFirst("userId")?.Value;

        if (userId != null)
        {
            lock (_lock)
            {
                _userPresence[Context.ConnectionId] = new UserPresence
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = Guid.Parse(userId),
                    UserName = Context.User?.Identity?.Name ?? "Unknown",
                    ConnectedAt = DateTimeOffset.UtcNow
                };
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? currentPageId = null;

        lock (_lock)
        {
            // Find which page this connection was viewing
            foreach (var (pageId, connections) in _pageConnections)
            {
                if (connections.Contains(Context.ConnectionId))
                {
                    currentPageId = pageId;
                    connections.Remove(Context.ConnectionId);

                    if (connections.Count == 0)
                    {
                        _pageConnections.Remove(pageId);
                    }
                    break;
                }
            }

            _userPresence.Remove(Context.ConnectionId);
        }

        // Notify others in the page that this user left
        if (currentPageId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, currentPageId);
            await Clients.Group(currentPageId).SendAsync("UserLeft", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a document session to start receiving real-time updates.
    /// </summary>
    public async Task JoinDocument(string pageId)
    {
        if (!Guid.TryParse(pageId, out var pageGuid))
        {
            throw new HubException("Invalid page ID format");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, pageId);

        lock (_lock)
        {
            if (!_pageConnections.ContainsKey(pageId))
            {
                _pageConnections[pageId] = new HashSet<string>();
            }
            _pageConnections[pageId].Add(Context.ConnectionId);
        }

        var presence = GetPagePresence(pageId);

        // Load persisted CRDT state snapshot from Redis
        // Note: We store a single merged state snapshot, not individual updates
        var snapshotKey = $"crdt:snapshot:{pageId}";
        var persistedSnapshot = await _cache.GetAsync(snapshotKey);

        int[] initialState;
        if (persistedSnapshot != null && persistedSnapshot.Length > 0)
        {
            // Send persisted CRDT snapshot to sync with other clients
            initialState = Array.ConvertAll(persistedSnapshot, b => (int)b);
        }
        else
        {
            // No persisted state yet - send empty array
            // First client will load from DB and their updates will be persisted
            initialState = Array.Empty<int>();
        }

        await Clients.Caller.SendAsync("InitialState", initialState);

        // Notify the joiner about current users
        await Clients.Caller.SendAsync("CurrentUsers", presence);

        // Notify others in the group about the new user
        if (_userPresence.TryGetValue(Context.ConnectionId, out var userPresence))
        {
            await Clients.OthersInGroup(pageId).SendAsync("UserJoined", new
            {
                connectionId = Context.ConnectionId,
                userId = userPresence.UserId,
                userName = userPresence.UserName
            });
        }
    }

    /// <summary>
    /// Leave a document session to stop receiving updates.
    /// </summary>
    public async Task LeaveDocument(string pageId)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        lock (_lock)
        {
            if (_pageConnections.TryGetValue(pageId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                {
                    _pageConnections.Remove(pageId);
                }
            }
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, pageId);
        await Clients.Group(pageId).SendAsync("UserLeft", Context.ConnectionId);
    }

    /// <summary>
    /// Broadcast a page title update to all users viewing the page.
    /// </summary>
    public async Task BroadcastPageTitleUpdate(string pageId, string newTitle)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        await Clients.OthersInGroup(pageId).SendAsync("PageTitleUpdated", new
        {
            pageId,
            title = newTitle,
            updatedBy = Context.ConnectionId,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Broadcast a block addition to all users viewing the page.
    /// </summary>
    public async Task BroadcastBlockAdded(string pageId, object block)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        await Clients.OthersInGroup(pageId).SendAsync("BlockAdded", new
        {
            pageId,
            block,
            addedBy = Context.ConnectionId,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Broadcast a block update to all users viewing the page.
    /// </summary>
    public async Task BroadcastBlockUpdated(string pageId, string blockId, object updates)
    {
        if (!Guid.TryParse(pageId, out _) || !Guid.TryParse(blockId, out _))
        {
            throw new HubException("Invalid ID format");
        }

        await Clients.OthersInGroup(pageId).SendAsync("BlockUpdated", new
        {
            pageId,
            blockId,
            updates,
            updatedBy = Context.ConnectionId,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Broadcast a block removal to all users viewing the page.
    /// </summary>
    public async Task BroadcastBlockRemoved(string pageId, string blockId)
    {
        if (!Guid.TryParse(pageId, out _) || !Guid.TryParse(blockId, out _))
        {
            throw new HubException("Invalid ID format");
        }

        await Clients.OthersInGroup(pageId).SendAsync("BlockRemoved", new
        {
            pageId,
            blockId,
            removedBy = Context.ConnectionId,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Update cursor position for presence awareness.
    /// </summary>
    public async Task UpdateCursor(string pageId, object cursorPosition)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        await Clients.OthersInGroup(pageId).SendAsync("CursorMoved", new
        {
            connectionId = Context.ConnectionId,
            cursorPosition,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Update user's current selection for presence awareness.
    /// </summary>
    public async Task UpdateSelection(string pageId, object selection)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        await Clients.OthersInGroup(pageId).SendAsync("SelectionChanged", new
        {
            connectionId = Context.ConnectionId,
            selection,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Broadcast typing indicator to show which block a user is editing.
    /// </summary>
    public async Task UpdateTypingIndicator(string pageId, string? blockId, bool isTyping)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        if (blockId != null && !Guid.TryParse(blockId, out _))
        {
            throw new HubException("Invalid block ID format");
        }

        await Clients.OthersInGroup(pageId).SendAsync("TypingIndicatorChanged", new
        {
            connectionId = Context.ConnectionId,
            blockId,
            isTyping,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Relay CRDT updates between clients (server acts as message relay).
    /// </summary>
    public async Task SendUpdate(string pageId, int[] update)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        // Simply relay the update to other clients - Yjs handles CRDT on frontend
        // Note: We don't persist every update (too chatty). Instead, clients periodically
        // save snapshots using SaveSnapshot method.
        await Clients.OthersInGroup(pageId).SendAsync("ReceiveUpdate", update);
    }

    /// <summary>
    /// Save a snapshot of the current CRDT state to Redis for persistence.
    /// Clients should call this periodically (e.g., every 30 seconds) or when disconnecting.
    /// </summary>
    public async Task SaveSnapshot(string pageId, int[] stateSnapshot)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        try
        {
            var snapshotKey = $"crdt:snapshot:{pageId}";
            var options = new DistributedCacheEntryOptions
            {
                // Keep snapshots for 30 days of inactivity
                SlidingExpiration = TimeSpan.FromDays(30)
            };

            // Convert int[] to byte[] for storage
            var snapshotBytes = Array.ConvertAll(stateSnapshot, i => (byte)i);
            await _cache.SetAsync(snapshotKey, snapshotBytes, options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to persist CRDT snapshot for page {pageId}: {ex.Message}");
            throw new HubException("Failed to save document snapshot");
        }
    }

    /// <summary>
    /// Relay Yjs Awareness updates for cursor/selection synchronization.
    /// </summary>
    public async Task SendAwarenessUpdate(string pageId, int[] update)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        // Relay awareness updates (cursors, selections) to other clients
        await Clients.OthersInGroup(pageId).SendAsync("ReceiveAwarenessUpdate", update);
    }

    /// <summary>
    /// Update presence information (cursor position, etc.)
    /// </summary>
    public async Task UpdatePresence(string pageId, object presence)
    {
        if (!Guid.TryParse(pageId, out _))
        {
            throw new HubException("Invalid page ID format");
        }

        await Clients.OthersInGroup(pageId).SendAsync("PresenceUpdate", Context.ConnectionId, presence);
    }

    private List<object> GetPagePresence(string pageId)
    {
        lock (_lock)
        {
            if (!_pageConnections.TryGetValue(pageId, out var connections))
            {
                return new List<object>();
            }

            return connections
                .Where(connId => _userPresence.ContainsKey(connId))
                .Select(connId => new
                {
                    connectionId = connId,
                    userId = _userPresence[connId].UserId,
                    userName = _userPresence[connId].UserName,
                    connectedAt = _userPresence[connId].ConnectedAt
                })
                .ToList<object>();
        }
    }

    private class UserPresence
    {
        public string ConnectionId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTimeOffset ConnectedAt { get; set; }
    }
}
