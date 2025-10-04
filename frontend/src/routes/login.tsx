import { createFileRoute, useNavigate } from '@tanstack/react-router';
import { Auth } from '@/features/auth';
import { apiClient } from '@/lib/api/client';
import { useAuthStore } from '@/stores/auth-store';
import { toast } from 'sonner';

export const Route = createFileRoute('/login')({
  component: LoginPage,
});

function LoginPage() {
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);

  const handleLogin = async (values: Record<string, unknown>) => {
    try {
      // @ts-expect-error - API type generation mismatch
      const response = await apiClient.POST('/api/Auth/login', {
        body: {
          email: values.email as string,
          password: values.password as string,
        },
      });

      if (response.error) {
        throw new Error('Login failed');
      }

      const result = response.data as any;

      // Token is stored in HttpOnly cookie AND in memory for SSE/EventSource
      login({
        id: result.user.id,
        email: result.user.email,
        name: result.user.name,
      }, result.token);

      toast.success('Welcome back!', {
        description: `Signed in as ${result.user.email}`,
      });

      await navigate({ to: '/' });
    } catch (error) {
      toast.error('Login failed', {
        description: error instanceof Error ? error.message : 'Unable to connect to the server',
      });
      // Don't throw - let the form handle the error state
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <Auth.Provider onSubmit={handleLogin}>
        <Auth.Form className="w-full max-w-md">
          <Auth.Header description="Enter your credentials to continue">Welcome back</Auth.Header>

          <Auth.Content>
            <Auth.Field name="email" label="Email" type="email" placeholder="hello@example.com" />

            <Auth.Field name="password" label="Password" type="password" placeholder="••••••••" />

            <Auth.Submit>Sign in</Auth.Submit>

            <div className="text-center text-sm">
              <span className="text-muted-foreground">Don't have an account? </span>
              <Auth.Link onClick={() => navigate({ to: '/register' })}>Create one</Auth.Link>
            </div>
          </Auth.Content>
        </Auth.Form>
      </Auth.Provider>
    </div>
  );
}
