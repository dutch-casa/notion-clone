import { useForm } from '@tanstack/react-form';
import { createContext, type ReactNode, useContext } from 'react';

interface AuthContextValue {
  form: ReturnType<typeof useForm<{ email: string; password: string; name: string }>>;
  isSubmitting: boolean;
  error: string | null;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuthContext() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('Auth compound components must be wrapped in <Auth.Provider>');
  }
  return context;
}

interface AuthProviderProps {
  children: ReactNode;
  onSubmit: (values: Record<string, unknown>) => Promise<void>;
}

export function AuthProvider({ children, onSubmit }: AuthProviderProps) {
  const form = useForm({
    defaultValues: {
      email: '',
      password: '',
      name: '',
    },
    onSubmit: async ({ value }) => {
      await onSubmit(value);
    },
  });

  return (
    <AuthContext.Provider
      value={{
        form,
        isSubmitting: form.state.isSubmitting,
        error: null,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
