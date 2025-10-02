import { createFileRoute, useNavigate, redirect } from '@tanstack/react-router';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore } from '@/stores/auth-store';
import { apiClient } from '@/lib/api/client';
import { CreateOrgDialog } from '@/features/organizations/create-org-dialog';
import { Button } from '@/components/ui/button';
import { Plus, Users, Crown, User } from 'lucide-react';

export const Route = createFileRoute('/organizations/')({
  beforeLoad: () => {
    const { isAuthenticated } = useAuthStore.getState();
    if (!isAuthenticated) {
      throw redirect({ to: '/login' });
    }
  },
  component: OrganizationsPage,
});

function OrganizationsPage() {
  const navigate = useNavigate();

  // Fetch organizations with React Query
  const { data: organizations = [], isLoading } = useQuery({
    queryKey: ['organizations'],
    queryFn: async () => {
      const response = await apiClient.GET('/api/Organizations');
      if (response.error) {
        throw new Error('Failed to fetch organizations');
      }
      return response.data || [];
    },
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-gray-500">Loading organizations...</div>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
        <h1 className="text-2xl font-semibold text-gray-900">Organizations</h1>
        <CreateOrgDialog
          trigger={
            <Button variant="outline" className="gap-2">
              <Plus className="w-4 h-4" />
              New organization
            </Button>
          }
        />
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto px-6 py-4">
        {organizations.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <Users className="w-12 h-12 text-gray-300 mb-4" />
            <div className="text-gray-500">
              <p className="text-base font-medium mb-1">No organizations yet</p>
              <p className="text-sm">Create your first organization to get started</p>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {organizations.map((org) => (
              <button
                key={org.id}
                onClick={() => navigate({ to: '/organizations/$orgId', params: { orgId: org.id! } })}
                className="group relative flex flex-col gap-3 p-5 rounded-lg border border-gray-200 hover:border-gray-300 hover:shadow-sm transition-all bg-white text-left"
              >
                <div className="flex items-start gap-3">
                  <div className="flex-shrink-0 w-10 h-10 rounded bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-semibold text-lg">
                    {org.name?.charAt(0).toUpperCase() ?? 'O'}
                  </div>
                  <div className="flex-1 min-w-0">
                    <h3 className="font-semibold text-gray-900 truncate">{org.name}</h3>
                    <div className="flex items-center gap-2 mt-1">
                      {org.role === 'owner' ? (
                        <span className="inline-flex items-center gap-1 text-xs text-amber-700">
                          <Crown className="w-3 h-3" />
                          Owner
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-xs text-gray-500">
                          <User className="w-3 h-3" />
                          {org.role}
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
