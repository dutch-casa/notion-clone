import { useEffect, useRef } from 'react';
import { useAuthStore } from '@/stores/auth-store';
import { API_BASE_URL } from '@/lib/config';

export interface InvitationNotification {
  invitationId: string;
  orgId: string;
  orgName: string;
  inviterUserId: string;
  inviterName: string;
  role: string;
  createdAt: string;
}

interface UseInvitationNotificationsOptions {
  onNotification?: (notification: InvitationNotification) => void;
  enabled?: boolean;
}

/**
 * Hook to connect to SSE endpoint for real-time invitation notifications
 */
export function useInvitationNotifications({
  onNotification,
  enabled = true,
}: UseInvitationNotificationsOptions = {}) {
  const eventSourceRef = useRef<EventSource | null>(null);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const mountedRef = useRef(true);
  const user = useAuthStore((state) => state.user);
  const token = useAuthStore((state) => state.token);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  useEffect(() => {
    // Only connect if enabled and user is authenticated
    if (!enabled || !user || !token) {
      return;
    }

    let reconnectAttempts = 0;
    const maxReconnectAttempts = 5;
    const baseReconnectDelay = 1000; // 1 second

    const connect = () => {
      if (!mountedRef.current) return;

      try {
        // Create SSE connection with token in query string (EventSource can't send cookies cross-origin)
        const url = `${API_BASE_URL}/api/Organizations/invitations/stream?access_token=${encodeURIComponent(token)}`;
        const eventSource = new EventSource(url, {
          withCredentials: true, // Still needed for CORS
        });

        eventSourceRef.current = eventSource;

        eventSource.onopen = () => {
          reconnectAttempts = 0; // Reset reconnect attempts on successful connection
        };

        eventSource.onmessage = (event) => {
          try {
            const notification: InvitationNotification = JSON.parse(event.data);
            onNotification?.(notification);
          } catch (error) {
            console.error('[InvitationNotifications] Failed to parse notification:', error);
          }
        };

        eventSource.onerror = (error) => {
          console.error('[InvitationNotifications] SSE error:', error);
          eventSource.close();
          eventSourceRef.current = null;

          // Attempt to reconnect with exponential backoff
          if (mountedRef.current && reconnectAttempts < maxReconnectAttempts) {
            const delay = baseReconnectDelay * Math.pow(2, reconnectAttempts);
            reconnectAttempts++;

            reconnectTimeoutRef.current = setTimeout(() => {
              if (mountedRef.current) {
                connect();
              }
            }, delay);
          }
        };
      } catch (error) {
        console.error('[InvitationNotifications] Failed to create EventSource:', error);
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
  }, [enabled, user, token, onNotification]);

  return {
    isConnected: !!eventSourceRef.current && eventSourceRef.current.readyState === EventSource.OPEN,
  };
}
