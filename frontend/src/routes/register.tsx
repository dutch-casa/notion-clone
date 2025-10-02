import { createFileRoute, useNavigate } from '@tanstack/react-router';
import { Auth } from '@/features/auth';
import { apiClient } from '@/lib/api/client';
import { useAuthStore } from '@/stores/auth-store';
import { toast } from 'sonner';

export const Route = createFileRoute('/register')({
  component: RegisterPage,
});

function RegisterPage() {
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);

  const handleRegister = async (values: Record<string, unknown>) => {
    try {
      // @ts-expect-error - API type generation mismatch
      const response = await apiClient.POST('/api/Auth/register', {
        body: {
          email: values.email as string,
          password: values.password as string,
          name: values.name as string,
        },
      });

      if (response.error) {
        throw new Error('Registration failed');
      }

      const result = response.data as any;

      // Token is now stored in HttpOnly cookie by the backend
      login({
        id: result.user.id,
        email: result.user.email,
        name: result.user.name,
      });

      toast.success('Account created!', {
        description: `Welcome, ${result.user.name}!`,
      });

      await navigate({ to: '/' });
    } catch (error) {
      toast.error('Registration failed', {
        description: error instanceof Error ? error.message : 'Unable to connect to the server',
      });
      // Don't throw - let the form handle the error state
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <Auth.Provider onSubmit={handleRegister}>
        <Auth.Form className="w-full max-w-md">
          <Auth.Header description="Create an account to get started">Create account</Auth.Header>

          <Auth.Content>
            <Auth.Field name="name" label="Name" type="text" placeholder="John Doe" />

            <Auth.Field name="email" label="Email" type="email" placeholder="hello@example.com" />

            <Auth.Field name="password" label="Password" type="password" placeholder="••••••••" />

            <Auth.Submit>Create account</Auth.Submit>

            <div className="text-center text-sm">
              <span className="text-muted-foreground">Already have an account? </span>
              <Auth.Link onClick={() => navigate({ to: '/login' })}>Sign in</Auth.Link>
            </div>
          </Auth.Content>
        </Auth.Form>
      </Auth.Provider>
    </div>
  );
}
