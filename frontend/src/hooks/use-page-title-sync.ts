import { useEffect, useRef, useCallback } from 'react';

interface PageTitleSyncMessage {
  type: 'title-update';
  pageId: string;
  title: string;
  timestamp: number;
}

/**
 * Hook to sync page titles bidirectionally across browser contexts (tabs, windows)
 * using the Broadcast Channel API. Includes error handling and graceful degradation.
 *
 * @param pageId - The ID of the page to sync
 * @param onTitleReceived - Callback when a title update is received from another context
 */
export function usePageTitleSync(
  pageId: string,
  onTitleReceived?: (title: string) => void
) {
  const channelRef = useRef<BroadcastChannel | null>(null);
  const lastBroadcastRef = useRef<string | null>(null);
  const isSupported = useRef(typeof BroadcastChannel !== 'undefined');
  const onTitleReceivedRef = useRef(onTitleReceived);

  // Keep ref updated
  useEffect(() => {
    onTitleReceivedRef.current = onTitleReceived;
  }, [onTitleReceived]);

  // Initialize the broadcast channel
  useEffect(() => {
    if (!isSupported.current) {
      console.warn('BroadcastChannel API is not supported in this browser');
      return;
    }

    const channelName = `page-title-sync-${pageId}`;

    try {
      const channel = new BroadcastChannel(channelName);
      channelRef.current = channel;

      // Listen for messages from other contexts
      channel.onmessage = (event: MessageEvent<PageTitleSyncMessage>) => {
        const { type, pageId: messagePageId, title } = event.data;

        if (type === 'title-update' && messagePageId === pageId) {
          // Only apply if we didn't just broadcast this same value
          if (title !== lastBroadcastRef.current) {
            onTitleReceivedRef.current?.(title);
          }
        }
      };

      channel.onerror = (error) => {
        console.error('[PageTitleSync] Broadcast channel error:', error);
      };
    } catch (error) {
      console.error('[PageTitleSync] Failed to create BroadcastChannel:', error);
      isSupported.current = false;
    }

    // Cleanup
    return () => {
      if (channelRef.current) {
        try {
          channelRef.current.close();
          channelRef.current = null;
        } catch (error) {
          console.error('[PageTitleSync] Error closing channel:', error);
        }
      }
    };
  }, [pageId]);

  // Broadcast title changes to other contexts
  const broadcastTitle = useCallback((title: string) => {
    if (!isSupported.current || !channelRef.current) {
      return;
    }

    try {
      const message: PageTitleSyncMessage = {
        type: 'title-update',
        pageId,
        title,
        timestamp: Date.now(),
      };

      channelRef.current.postMessage(message);
      lastBroadcastRef.current = title;
    } catch (error) {
      console.error('[PageTitleSync] Failed to broadcast message:', error);
    }
  }, [pageId]);

  return {
    broadcastTitle,
    isSupported: isSupported.current,
  };
}
