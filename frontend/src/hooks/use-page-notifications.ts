import { useEffect, useRef } from 'react';
import { useAuthStore } from '@/stores/auth-store';
import { API_BASE_URL } from '@/lib/config';

export interface PageNotification {
  pageId: string;
  title: string; // The page title (newTitle for rename events)
  orgId: string;
  eventType: 'PageCreated' | 'PageTitleChanged' | 'PageDeleted';
  actorUserId: string;
  timestamp: string;
  oldTitle?: string; // For PageTitleChanged events
}

interface UsePageNotificationsOptions {
  orgId: string;
  onNotification?: (notification: PageNotification) => void;
  enabled?: boolean;
}

/**
 * Hook to connect to SSE endpoint for real-time page notifications (create, rename, delete)
 * Follows DDD + Clean Architecture patterns - domain events dispatched from aggregate roots
 */
export function usePageNotifications({
  orgId,
  onNotification,
  enabled = true,
}: UsePageNotificationsOptions) {
  const eventSourceRef = useRef<EventSource | null>(null);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const mountedRef = useRef(true);
  const user = useAuthStore((state) => state.user);
  const token = useAuthStore((state) => state.token);
  const fetchToken = useAuthStore((state) => state.fetchToken);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  useEffect(() => {
    // Only connect if enabled, user is authenticated, and orgId is provided
    if (!enabled || !user || !orgId) {
      return;
    }

    // If user exists but token is missing, fetch it from cookie
    if (!token) {
      fetchToken();
      return;
    }

    let reconnectAttempts = 0;
    const maxReconnectAttempts = 5;
    const baseReconnectDelay = 1000; // 1 second

    const connect = () => {
      if (!mountedRef.current) return;

      try {
        // Create SSE connection with token in query string (EventSource can't send cookies cross-origin)
        const url = `${API_BASE_URL}/api/Pages/stream?orgId=${orgId}&access_token=${encodeURIComponent(token)}`;

        const eventSource = new EventSource(url, {
          withCredentials: true, // Still needed for CORS
        });

        eventSourceRef.current = eventSource;

        eventSource.onopen = () => {
          reconnectAttempts = 0; // Reset reconnect attempts on successful connection
          console.log(`[PageNotifications] Connected to org ${orgId}`);
        };

        eventSource.onmessage = (event) => {
          try {
            const notification: PageNotification = JSON.parse(event.data);
            console.log('[PageNotifications] Received:', notification.eventType, notification.title);
            onNotification?.(notification);
          } catch (error) {
            console.error('[PageNotifications] Failed to parse notification:', error);
          }
        };

        eventSource.onerror = (error) => {
          console.error('[PageNotifications] SSE error:', error);
          eventSource.close();
          eventSourceRef.current = null;

          // Attempt to reconnect with exponential backoff
          if (mountedRef.current && reconnectAttempts < maxReconnectAttempts) {
            const delay = baseReconnectDelay * Math.pow(2, reconnectAttempts);
            reconnectAttempts++;

            console.log(`[PageNotifications] Reconnecting in ${delay}ms (attempt ${reconnectAttempts}/${maxReconnectAttempts})`);

            reconnectTimeoutRef.current = setTimeout(() => {
              if (mountedRef.current) {
                connect();
              }
            }, delay);
          } else if (reconnectAttempts >= maxReconnectAttempts) {
            console.error('[PageNotifications] Max reconnect attempts reached');
          }
        };
      } catch (error) {
        console.error('[PageNotifications] Failed to create EventSource:', error);
      }
    };

    // Initial connection
    connect();

    // Cleanup
    return () => {
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
        reconnectTimeoutRef.current = null;
      }
    };
  }, [enabled, user, orgId, token, onNotification, fetchToken]);

  return {
    isConnected: !!eventSourceRef.current && eventSourceRef.current.readyState === EventSource.OPEN,
  };
}
