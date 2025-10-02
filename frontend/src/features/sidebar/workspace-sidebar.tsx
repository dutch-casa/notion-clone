import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { apiClient } from '@/lib/api/client';
import { useAuthStore } from '@/stores/auth-store';
import { usePageTitleSync } from '@/hooks/use-page-title-sync';
import { useInvitationNotifications } from '@/hooks/use-invitation-notifications';
import { usePageNotifications } from '@/hooks/use-page-notifications';
import { toast } from 'sonner';
import type { PageSummary } from '@/lib/api/types';
import {
  SidebarContainer,
  SidebarHeader,
  SidebarToggle,
  SidebarContent,
  SidebarSection,
  SidebarItem,
  SidebarEditableItem,
  SidebarSeparator,
  SidebarFooter,
} from './components';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { FileText, Settings, Plus, LogOut, ChevronDown, User, Inbox } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { WorkspaceSelector } from './workspace-selector';
import { AccountSettingsDialog } from '@/features/settings/account-settings-dialog';
import { InboxDialog } from '@/features/invitations/inbox-dialog';

// Separate component for page list items to properly use hooks
interface PageListItemProps {
  page: { id: string; title: string };
  orgId: string;
  pageId?: string;
  onNavigate: (pageId: string) => void;
  onUpdate: (pageId: string, title: string) => void;
  onDelete: (pageId: string) => void;
  canDelete: boolean;
  queryClient: QueryClient;
}

function PageListItem({ page, orgId, pageId, onNavigate, onUpdate, onDelete, canDelete, queryClient }: PageListItemProps) {
  // Individual sync hook for each page in sidebar
  const { broadcastTitle } = usePageTitleSync(
    page.id,
    useCallback((newTitle: string) => {
      // Update local state when receiving broadcast from editor
      queryClient.setQueryData(['pages', orgId], (old: PageSummary[] | undefined) => {
        if (!old) return old;
        return old.map(p =>
          p.id === page.id ? { ...p, title: newTitle } : p
        );
      });
    }, [page.id, queryClient, orgId])
  );

  return (
    <SidebarEditableItem
      value={page.title}
      icon={<FileText className="h-4 w-4" />}
      active={pageId === page.id}
      onClick={() => onNavigate(page.id)}
      onSave={(newTitle) => {
        onUpdate(page.id, newTitle);
        // Broadcast to other contexts (e.g., editor)
        broadcastTitle(newTitle);
      }}
      onDelete={() => onDelete(page.id)}
      canDelete={canDelete}
    />
  );
}

export function WorkspaceSidebar() {
  const navigate = useNavigate();
  const params = useParams({ strict: false });
  const { user, logout } = useAuthStore();
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [inboxOpen, setInboxOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [pageToDelete, setPageToDelete] = useState<{ id: string; title: string } | null>(null);
  const queryClient = useQueryClient();
  const orgId = 'orgId' in params ? params.orgId : undefined;
  const pageId = 'pageId' in params ? params.pageId : undefined;

  // Listen for real-time invitation notifications
  useInvitationNotifications({
    onNotification: useCallback((notification) => {
      // Show toast notification
      toast.info(`New invitation from ${notification.inviterName} to join ${notification.orgName}`);

      // Invalidate invitations query to refresh the badge count
      queryClient.invalidateQueries({ queryKey: ['invitations'] });
    }, [queryClient]),
  });

  // Listen for real-time page notifications (create, rename, delete)
  usePageNotifications({
    orgId: orgId || '',
    enabled: !!orgId,
    onNotification: useCallback((notification) => {
      // Handle different event types
      switch (notification.eventType) {
        case 'PageCreated':
          // Invalidate pages query to fetch the new page
          queryClient.invalidateQueries({ queryKey: ['pages', orgId] });
          toast.success(`New page "${notification.title}" created`);
          break;

        case 'PageTitleChanged':
          // Optimistically update the page title in the cache
          queryClient.setQueryData(['pages', orgId], (old: PageSummary[] | undefined) => {
            if (!old) return old;
            return old.map(p =>
              p.id === notification.pageId
                ? { ...p, title: notification.title }
                : p
            );
          });
          toast.info(`Page renamed from "${notification.oldTitle}" to "${notification.title}"`);
          break;

        case 'PageDeleted':
          // Remove the deleted page from the cache
          queryClient.setQueryData(['pages', orgId], (old: PageSummary[] | undefined) => {
            if (!old) return old;
            return old.filter(p => p.id !== notification.pageId);
          });
          toast.warning(`Page "${notification.title}" deleted`);

          // Navigate away if we're currently viewing the deleted page
          if (pageId === notification.pageId) {
            navigate({ to: '/organizations/$orgId', params: { orgId: orgId! } });
          }
          break;
      }
    }, [queryClient, orgId, pageId, navigate]),
  });

  // Fetch organizations
  const { data: organizations = [] } = useQuery({
    queryKey: ['organizations'],
    queryFn: async () => {
      const response = await apiClient.GET('/api/Organizations');
      if (response.error) throw new Error('Failed to fetch organizations');
      return response.data || [];
    },
  });

  // Fetch pages for current org
  const { data: pagesData } = useQuery({
    queryKey: ['pages', orgId],
    queryFn: async () => {
      if (!orgId) return [];
      const response = await apiClient.GET('/api/Pages', {
        params: { query: { orgId } },
      });
      if (response.error) throw new Error('Failed to fetch pages');
      return response.data?.pages || [];
    },
    enabled: !!orgId,
  });

  const pages = Array.isArray(pagesData) ? pagesData : [];

  const currentOrg = organizations.find(org => org.id === orgId);

  // Check if user can delete pages (only owner and admin)
  // The role is available directly on the organization object from the list endpoint
  const userRole = currentOrg?.role;
  const canDeletePages = userRole === 'owner' || userRole === 'admin';

  // Fetch invitations count for badge
  const { data: invitations = [] } = useQuery({
    queryKey: ['invitations'],
    queryFn: async () => {
      const response = await apiClient.GET('/api/Organizations/invitations');
      if (response.error) return [];
      return response.data || [];
    },
  });

  const createPageMutation = useMutation({
    mutationFn: async () => {
      if (!orgId) throw new Error('No organization selected');
      const response = await apiClient.POST('/api/Pages', {
        body: {
          orgId,
          title: 'Untitled',
        },
      });
      if (response.error) throw new Error('Failed to create page');
      return response.data;
    },
    onSuccess: async (newPage) => {
      await queryClient.invalidateQueries({ queryKey: ['pages', orgId] });
      await queryClient.refetchQueries({ queryKey: ['pages', orgId] });
      navigate({ to: '/organizations/$orgId/pages/$pageId', params: { orgId: orgId!, pageId: newPage.id } });
      toast.success('Page created');
    },
    onError: () => {
      toast.error('Failed to create page');
    },
  });

  const updatePageTitleMutation = useMutation({
    mutationFn: async ({ pageId, title }: { pageId: string; title: string }) => {
      const response = await apiClient.PATCH('/api/Pages/{id}/title', {
        params: { path: { id: pageId } },
        body: { title },
      });
      if (response.error) throw new Error('Failed to update page title');
      return response.data;
    },
    onMutate: async ({ pageId, title }) => {
      // Cancel any outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['pages', orgId] });

      // Snapshot the previous value
      const previousPages = queryClient.getQueryData(['pages', orgId]);

      // Optimistically update to the new value
      queryClient.setQueryData(['pages', orgId], (old: PageSummary[] | undefined) => {
        if (!old) return old;
        return old.map(page =>
          page.id === pageId ? { ...page, title } : page
        );
      });

      return { previousPages };
    },
    onError: (err, variables, context) => {
      // Rollback on error
      if (context?.previousPages) {
        queryClient.setQueryData(['pages', orgId], context.previousPages);
      }
      toast.error('Failed to rename page');
    },
    onSuccess: () => {
      toast.success('Page renamed');
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['pages', orgId] });
    },
  });

  const deletePageMutation = useMutation({
    mutationFn: async (pageId: string) => {
      const response = await apiClient.DELETE('/api/Pages/{id}', {
        params: { path: { id: pageId } },
      });
      if (response.error) throw new Error('Failed to delete page');
      return response.data;
    },
    onSuccess: async (_, pageId) => {
      await queryClient.invalidateQueries({ queryKey: ['pages', orgId] });
      toast.success('Page deleted');
      // Navigate away if we deleted the current page
      if (pageId === pageId) {
        navigate({ to: '/organizations/$orgId', params: { orgId: orgId! } });
      }
    },
    onError: () => {
      toast.error('Failed to delete page');
    },
  });

  return (
    <SidebarContainer>
      {/* Header - Workspace Selector */}
      <SidebarHeader>
        <WorkspaceSelector organizations={organizations} currentOrg={currentOrg} />
        <SidebarToggle />
      </SidebarHeader>

      <SidebarSeparator />

      {/* Content */}
      <SidebarContent>
        {/* Organization Settings (only show if org is selected) */}
        {orgId && (
          <>
            <SidebarSection>
              <SidebarItem
                icon={<Settings className="h-4 w-4" />}
                onClick={() => navigate({ to: '/organizations/$orgId', params: { orgId } })}
              >
                Organization Settings
              </SidebarItem>
            </SidebarSection>

            <SidebarSeparator />
          </>
        )}

        {/* Pages Section (only show if org is selected) */}
        {orgId && (
          <>
            <SidebarSection title="Pages">
              {pages.map((page) => (
                <PageListItem
                  key={page.id}
                  page={page}
                  orgId={orgId}
                  pageId={pageId}
                  onNavigate={(id) => {
                    navigate({ to: '/organizations/$orgId/pages/$pageId', params: { orgId, pageId: id } });
                  }}
                  onUpdate={(id, title) => {
                    updatePageTitleMutation.mutate({ pageId: id, title });
                  }}
                  onDelete={(id) => {
                    const page = pages.find(p => p.id === id);
                    if (page) {
                      setPageToDelete(page);
                      setDeleteDialogOpen(true);
                    }
                  }}
                  canDelete={canDeletePages}
                  queryClient={queryClient}
                />
              ))}
              <SidebarItem
                icon={<Plus className="h-4 w-4" />}
                onClick={() => createPageMutation.mutate()}
                className="text-gray-500 dark:text-gray-500 hover:text-gray-900 dark:hover:text-gray-100"
              >
                New page
              </SidebarItem>
            </SidebarSection>
          </>
        )}
      </SidebarContent>

      {/* Footer */}
      <SidebarFooter>
        {/* Inbox button */}
        <div className="mb-2">
          <SidebarItem
            icon={<Inbox className="h-4 w-4" />}
            onClick={() => setInboxOpen(true)}
          >
            <div className="flex items-center justify-between flex-1">
              <span>Inbox</span>
              {invitations.length > 0 && (
                <Badge variant="default" className="ml-auto">
                  {invitations.length}
                </Badge>
              )}
            </div>
          </SidebarItem>
        </div>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button className="flex items-center gap-2 w-full hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md px-2 py-2 transition-colors">
              <div className="flex-shrink-0 w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center text-white font-medium">
                <User className="h-4 w-4" />
              </div>
              <div className="flex-1 min-w-0 text-left">
                <div className="text-sm font-medium truncate text-gray-900 dark:text-gray-100">{user?.name}</div>
                <div className="text-xs text-gray-500 dark:text-gray-400 truncate">{user?.email}</div>
              </div>
              <ChevronDown className="h-4 w-4 text-gray-500 dark:text-gray-400 flex-shrink-0" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <DropdownMenuItem onClick={() => setSettingsOpen(true)}>
              <Settings className="h-4 w-4 mr-2" />
              Account Settings
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={logout}>
              <LogOut className="h-4 w-4 mr-2" />
              Sign out
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarFooter>

      <AccountSettingsDialog open={settingsOpen} onOpenChange={setSettingsOpen} />
      <InboxDialog open={inboxOpen} onOpenChange={setInboxOpen} />

      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete page</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{pageToDelete?.title}"? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (pageToDelete) {
                  deletePageMutation.mutate(pageToDelete.id);
                  setDeleteDialogOpen(false);
                  setPageToDelete(null);
                }
              }}
              className="bg-red-600 hover:bg-red-700 focus:ring-red-600"
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </SidebarContainer>
  );
}
