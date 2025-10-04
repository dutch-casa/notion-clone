import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface User {
  id: string;
  email: string;
  name: string;
}

interface AuthState {
  user: User | null;
  token: string | null; // Store token for SSE/EventSource (can't use HttpOnly cookie)
  isAuthenticated: boolean;

  login: (user: User, token: string) => void;
  logout: () => void;
  updateUser: (user: Partial<User>) => void;
  fetchToken: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      isAuthenticated: false,

      login: (user, token) => {
        // Token stored in memory for SSE/EventSource (HttpOnly cookie can't be accessed by JS)
        set({ user, token, isAuthenticated: true });
      },

      logout: async () => {
        // Call backend to clear HttpOnly cookie
        try {
          await fetch(`${import.meta.env.VITE_API_URL || 'http://localhost:5036'}/api/auth/logout`, {
            method: 'POST',
            credentials: 'include',
          });
        } catch (error) {
          console.error('Logout error:', error);
        }
        set({ user: null, token: null, isAuthenticated: false });
      },

      updateUser: (updatedFields) => {
        set((state) => ({
          user: state.user ? { ...state.user, ...updatedFields } : null,
        }));
      },

      fetchToken: async () => {
        try {
          const response = await fetch(`${import.meta.env.VITE_API_URL || 'http://localhost:5036'}/api/auth/token`, {
            credentials: 'include',
          });

          if (response.ok) {
            const data = await response.json();
            set({ token: data.token });
          }
        } catch (error) {
          console.error('Failed to fetch token:', error);
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
        // Don't persist token to localStorage for security
      }),
    }
  )
);
