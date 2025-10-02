import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface User {
  id: string;
  email: string;
  name: string;
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;

  login: (user: User) => void;
  logout: () => void;
  updateUser: (user: Partial<User>) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,

      login: (user) => {
        // Token is now stored in HttpOnly cookie, not in localStorage
        set({ user, isAuthenticated: true });
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
        set({ user: null, isAuthenticated: false });
      },

      updateUser: (updatedFields) => {
        set((state) => ({
          user: state.user ? { ...state.user, ...updatedFields } : null,
        }));
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
        // No longer storing token in localStorage
      }),
    }
  )
);
