import { createFileRoute, Link, useNavigate } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';
import { useAuthStore } from '@/stores/auth-store';
import { useEffect } from 'react';

export const Route = createFileRoute('/')({
  component: HomeComponent,
});

function HomeComponent() {
  const navigate = useNavigate();
  const { isAuthenticated, user, logout } = useAuthStore();

  useEffect(() => {
    if (isAuthenticated) {
      navigate({ to: '/organizations' });
    }
  }, [isAuthenticated, navigate]);

  if (isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center space-y-4">
          <h1 className="text-4xl font-bold">Welcome, {user?.name}!</h1>
          <p className="text-muted-foreground">{user?.email}</p>
          <Button onClick={logout} variant="outline">
            Sign out
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="text-center space-y-6">
        <div>
          <h1 className="text-4xl font-bold">Notion Clone</h1>
          <p className="mt-4 text-muted-foreground">
            A collaborative document editor built with DDD & CRDT
          </p>
        </div>

        <div className="flex gap-4 justify-center">
          <Button asChild>
            <Link to="/login">Sign in</Link>
          </Button>
          <Button asChild variant="outline">
            <Link to="/register">Create account</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
