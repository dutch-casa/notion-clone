import { createRootRoute, Outlet, useLocation } from '@tanstack/react-router';
import { TanStackRouterDevtools } from '@tanstack/react-router-devtools';
import { Toaster } from '@/components/ui/sonner';
import { AppLayout } from '@/layouts/app-layout';
import { useAuthStore } from '@/stores/auth-store';

export const Route = createRootRoute({
  component: RootComponent,
});

function RootComponent() {
  const location = useLocation();
  const { isAuthenticated } = useAuthStore();

  // Routes that should NOT have the sidebar
  const noSidebarRoutes = ['/', '/login', '/register'];
  const shouldShowSidebar = isAuthenticated && !noSidebarRoutes.includes(location.pathname);

  return (
    <>
      {shouldShowSidebar ? (
        <AppLayout>
          <Outlet />
        </AppLayout>
      ) : (
        <Outlet />
      )}
      <Toaster />
      {import.meta.env.DEV && <TanStackRouterDevtools position="bottom-right" />}
    </>
  );
}
