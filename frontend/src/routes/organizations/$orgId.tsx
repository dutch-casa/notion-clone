import { createFileRoute, redirect, Outlet, useMatches } from '@tanstack/react-router';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore } from '@/stores/auth-store';
import { apiClient } from '@/lib/api/client';
import { useRemoveMember, useInviteByEmail } from '@/hooks/use-organization-mutations';
import {
  OrgSettingsProvider,
  OrgSettingsHeader,
  OrgSettingsTitle,
  OrgSettingsContent,
  OrgSettingsSection,
  OrgMembersList,
  InviteByEmailForm,
} from '@/features/organizations/components';

export const Route = createFileRoute('/organizations/$orgId')({
  beforeLoad: () => {
    const { isAuthenticated } = useAuthStore.getState();
    if (!isAuthenticated) {
      throw redirect({ to: '/login' });
    }
  },
  component: OrganizationSettingsPage,
});

function OrganizationSettingsPage() {
  const { orgId } = Route.useParams();
  const { user } = useAuthStore();
  const matches = useMatches();

  // Fetch organization details (hook must be called before any returns)
  const { data: organization, isLoading } = useQuery({
    queryKey: ['organization', orgId],
    queryFn: async () => {
      const response = await apiClient.GET('/api/Organizations/{id}', {
        params: { path: { id: orgId } },
      });
      if (response.error) {
        throw new Error('Failed to fetch organization');
      }
      return response.data;
    },
  });

  // Mutations
  const removeMemberMutation = useRemoveMember(orgId);
  const inviteByEmailMutation = useInviteByEmail(orgId);

  // Check if we're on a child route (like pages) - AFTER all hooks
  const hasChildRoute = matches.some(match =>
    match.routeId === '/organizations/$orgId/pages/$pageId'
  );

  // If on a child route, just render the outlet
  if (hasChildRoute) {
    return <Outlet />;
  }

  const handleRemoveMember = async (userId: string) => {
    if (confirm('Are you sure you want to remove this member?')) {
      removeMemberMutation.mutate(userId);
    }
  };

  const handleInviteByEmail = async (email: string, role: string) => {
    await inviteByEmailMutation.mutateAsync({ email, role });
  };

  if (isLoading || !organization) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-gray-500">Loading organization...</div>
      </div>
    );
  }

  // Find current user's role
  const currentUserRole = organization.members?.find(m => m.userId === user?.id)?.role;

  return (
    <OrgSettingsProvider
        orgId={orgId}
        orgName={organization.name}
        members={organization.members}
        currentUserRole={currentUserRole}
        onRemoveMember={handleRemoveMember}
      >
        <OrgSettingsHeader>
          <OrgSettingsTitle />
        </OrgSettingsHeader>

        <OrgSettingsContent>
          <OrgSettingsSection
            title="General"
            description="Basic information about this organization"
          >
            <div className="rounded-lg border border-gray-200 p-4">
              <div className="space-y-2">
                <div className="text-sm font-medium text-gray-700">Organization Name</div>
                <div className="text-base text-gray-900">{organization.name}</div>
              </div>
            </div>
          </OrgSettingsSection>

          <OrgSettingsSection
            title="Members"
            description="Manage who has access to this organization"
          >
            <div className="space-y-4">
              {(currentUserRole === 'owner' || currentUserRole === 'admin') && (
                <InviteByEmailForm onSubmit={handleInviteByEmail} />
              )}
              <OrgMembersList />
            </div>
          </OrgSettingsSection>
        </OrgSettingsContent>
      </OrgSettingsProvider>
  );
}
