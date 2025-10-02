# SignalR Hub Contracts

## Overview
Real-time collaboration via SignalR for CRDT synchronization and presence awareness.

---

## DocHub

### Connection
- **URL**: `/hubs/doc`
- **Authentication**: Bearer token required
- **Transport**: WebSockets (preferred), Server-Sent Events (fallback)

### Server Methods (Invokable by Clients)

#### JoinPage
```typescript
JoinPage(pageId: Guid): Promise<void>
```

**Purpose**: Subscribe to page updates and announce presence.

**Parameters**:
- `pageId`: UUID of page to join

**Behavior**:
- Adds connection to SignalR group `page:{pageId}`
- Validates user has view permission on page
- Broadcasts `PresenceUpdate` to existing group members (user joined)
- Returns current presence list to joining user

**Errors**:
- `Forbidden`: User doesn't have view permission
- `NotFound`: Page doesn't exist

**Example**:
```typescript
await connection.invoke("JoinPage", "3fa85f64-5717-4562-b3fc-2c963f66afa6");
```

---

#### LeavePage
```typescript
LeavePage(pageId: Guid): Promise<void>
```

**Purpose**: Unsubscribe from page updates and remove presence.

**Parameters**:
- `pageId`: UUID of page to leave

**Behavior**:
- Removes connection from SignalR group `page:{pageId}`
- Broadcasts `PresenceUpdate` to remaining group members (user left)

**Example**:
```typescript
await connection.invoke("LeavePage", "3fa85f64-5717-4562-b3fc-2c963f66afa6");
```

---

#### ApplyCrdtUpdate
```typescript
ApplyCrdtUpdate(pageId: Guid, updateB64: string, stateVectorB64?: string): Promise<number>
```

**Purpose**: Apply CRDT update and broadcast to other clients.

**Parameters**:
- `pageId`: UUID of page
- `updateB64`: Base64-encoded Yjs update (binary)
- `stateVectorB64`: Optional base64-encoded state vector for conflict resolution

**Behavior**:
- Validates user has edit permission on page
- Decodes updateB64 to byte[]
- Appends to `doc_states` table with atomic seq increment
- Broadcasts `CrdtUpdate` to group (excluding sender)
- Returns assigned seq number

**Returns**: `seq` (long) - Sequence number assigned to update

**Errors**:
- `Forbidden`: User doesn't have edit permission
- `BadRequest`: Invalid update format

**Example**:
```typescript
const update = new Uint8Array([...]); // Yjs update
const updateB64 = btoa(String.fromCharCode(...update));
const seq = await connection.invoke("ApplyCrdtUpdate", pageId, updateB64, null);
console.log(`Update persisted with seq ${seq}`);
```

---

#### UpdateCursor
```typescript
UpdateCursor(pageId: Guid, cursor: CursorDto): Promise<void>
```

**Purpose**: Broadcast cursor position to other clients (not persisted).

**Parameters**:
- `pageId`: UUID of page
- `cursor`: Cursor position data

**Behavior**:
- Validates user is in page group
- Broadcasts `CursorUpdate` to group (excluding sender)
- Not persisted (ephemeral)

**CursorDto**:
```typescript
{
  userId: string;    // User ID
  anchor: number;    // Selection anchor position
  head: number;      // Selection head position
  color: string;     // User's cursor color (hex)
}
```

**Example**:
```typescript
await connection.invoke("UpdateCursor", pageId, {
  userId: "user-id",
  anchor: 42,
  head: 50,
  color: "#FF5733"
});
```

---

### Client Methods (Invoked by Server)

#### PresenceUpdate
```typescript
PresenceUpdate(presence: PresenceDto): void
```

**Purpose**: Notify client of user join/leave events.

**PresenceDto**:
```typescript
{
  pageId: string;        // Page UUID
  userId: string;        // User UUID
  userName: string;      // Display name
  action: "joined" | "left";
  timestamp: string;     // ISO 8601 timestamp
  activeUsers: {         // Current active users
    userId: string;
    userName: string;
    color: string;
  }[];
}
```

**Example**:
```typescript
connection.on("PresenceUpdate", (presence) => {
  console.log(`${presence.userName} ${presence.action} the page`);
  // Update UI with active users
});
```

---

#### CrdtUpdate
```typescript
CrdtUpdate(pageId: string, updateB64: string): void
```

**Purpose**: Receive CRDT update from another client.

**Parameters**:
- `pageId`: Page UUID
- `updateB64`: Base64-encoded Yjs update

**Behavior**:
- Client decodes updateB64 to byte[]
- Applies update to local Yjs doc: `Y.applyUpdate(ydoc, update)`
- Triggers re-render of affected editor content

**Example**:
```typescript
connection.on("CrdtUpdate", (pageId, updateB64) => {
  const update = Uint8Array.from(atob(updateB64), c => c.charCodeAt(0));
  Y.applyUpdate(ydoc, update);
});
```

---

#### CursorUpdate
```typescript
CursorUpdate(pageId: string, cursor: CursorDto): void
```

**Purpose**: Receive cursor position from another client.

**Parameters**:
- `pageId`: Page UUID
- `cursor`: Cursor position data

**Behavior**:
- Client updates remote cursor overlay in editor
- Displays user name and color
- Ephemeral (not persisted)

**Example**:
```typescript
connection.on("CursorUpdate", (pageId, cursor) => {
  // Update remote cursor overlay
  renderCursor(cursor.userId, cursor.anchor, cursor.head, cursor.color);
});
```

---

## Connection Lifecycle

### Client Connection Flow
1. **Connect**: Establish WebSocket connection with Bearer token
   ```typescript
   const connection = new HubConnectionBuilder()
     .withUrl("/hubs/doc", {
       accessTokenFactory: () => getAuthToken()
     })
     .withAutomaticReconnect()
     .build();

   await connection.start();
   ```

2. **Join Page**: Subscribe to page group
   ```typescript
   await connection.invoke("JoinPage", pageId);
   ```

3. **Setup Listeners**: Register client method handlers
   ```typescript
   connection.on("CrdtUpdate", handleCrdtUpdate);
   connection.on("PresenceUpdate", handlePresence);
   connection.on("CursorUpdate", handleCursor);
   ```

4. **Send Updates**: Invoke server methods
   ```typescript
   ydoc.on("update", (update) => {
     const updateB64 = encodeBase64(update);
     connection.invoke("ApplyCrdtUpdate", pageId, updateB64);
   });
   ```

5. **Disconnect**: Clean up on page navigation
   ```typescript
   await connection.invoke("LeavePage", pageId);
   await connection.stop();
   ```

---

## Redis Backplane Configuration

For horizontal scaling, SignalR uses Redis backplane to synchronize messages across server instances.

**Configuration** (`appsettings.json`):
```json
{
  "Redis": {
    "Connection": "localhost:6379",
    "ChannelPrefix": "DocHub"
  },
  "SignalR": {
    "MaximumReceiveMessageSize": 102400,
    "HandshakeTimeout": "00:00:30",
    "KeepAliveInterval": "00:00:15",
    "MaximumParallelInvocations": 1
  }
}
```

**Registration** (`Program.cs`):
```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(options => {
        options.ConnectionFactory = async writer => {
            var config = ConfigurationOptions.Parse(redisConnectionString);
            config.ChannelPrefix = "DocHub";
            return await ConnectionMultiplexer.ConnectAsync(config, writer);
        };
    });
```

---

## Error Handling

### Connection Errors
- **401 Unauthorized**: Invalid or expired Bearer token → client redirects to login
- **503 Service Unavailable**: SignalR hub overloaded → client retries with exponential backoff

### Invocation Errors
- **Forbidden**: Permission denied → client shows error toast
- **BadRequest**: Invalid arguments → client logs error
- **NotFound**: Page doesn't exist → client redirects to org page list

### Reconnection
- Automatic reconnection enabled with exponential backoff
- On reconnect: client re-joins page and fetches latest CRDT state

**Example**:
```typescript
connection.onreconnected(async () => {
  await connection.invoke("JoinPage", pageId);
  // Fetch latest CRDT state to sync missed updates
  const stateB64 = await fetch(`/pages/${pageId}/crdt:state`).then(r => r.text());
  const state = decodeBase64(stateB64);
  Y.applyUpdate(ydoc, state);
});
```

---

## Performance Considerations

### Throttling Outbound Updates
Client-side throttling prevents flooding the server:
```typescript
let updateBuffer: Uint8Array[] = [];
let flushTimeout: NodeJS.Timeout | null = null;

ydoc.on("update", (update) => {
  updateBuffer.push(update);

  if (flushTimeout) clearTimeout(flushTimeout);

  flushTimeout = setTimeout(() => {
    const merged = Y.mergeUpdates(updateBuffer);
    const updateB64 = encodeBase64(merged);
    connection.invoke("ApplyCrdtUpdate", pageId, updateB64);
    updateBuffer = [];
  }, 100); // Flush every 100ms
});
```

### Message Size Limits
- Max message size: 100KB (configured in SignalR options)
- Large updates chunked or trigger full state sync

### Backpressure
- Server monitors message queue per connection
- If queue exceeds threshold, server drops connection

---

## Testing

### Unit Tests
- Mock `IHubCallerClients` to verify broadcasts
- Test authorization checks in hub methods

### Integration Tests
- Use `HubConnectionBuilder` with `TestServer`
- Simulate multi-client scenarios

**Example**:
```csharp
[Fact]
public async Task ApplyCrdtUpdate_BroadcastsToOtherClients()
{
    // Arrange
    var client1 = CreateTestClient();
    var client2 = CreateTestClient();
    await client1.InvokeAsync("JoinPage", pageId);
    await client2.InvokeAsync("JoinPage", pageId);

    var receivedUpdate = new TaskCompletionSource<string>();
    client2.On<string, string>("CrdtUpdate", (pid, updateB64) => {
        receivedUpdate.SetResult(updateB64);
    });

    // Act
    await client1.InvokeAsync("ApplyCrdtUpdate", pageId, "base64update", null);

    // Assert
    var result = await receivedUpdate.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Equal("base64update", result);
}
```
