import { createFileRoute } from '@tanstack/react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import { BlockEditor } from '@/features/editor/block-editor';
import { useState, useEffect, useLayoutEffect, useRef, useCallback } from 'react';
import { Loader2, Users } from 'lucide-react';
import { CollaborationService, getUserColor } from '@/lib/collaboration';
import { useAuthStore } from '@/stores/auth-store';
import { API_BASE_URL } from '@/lib/config';
import { useUpdatePageTitle } from '@/hooks/use-page-mutations';
import { usePageTitleSync } from '@/hooks/use-page-title-sync';
import { toast } from 'sonner';
import type { PageDto } from '@/lib/api/types';

export const Route = createFileRoute(
  '/organizations/$orgId/pages/$pageId'
)({
  component: PageEditor,
});

function PageEditor() {
  const { orgId, pageId } = Route.useParams();
  const [localTitle, setLocalTitle] = useState('');
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);

  // Enable collaboration mode if user is authenticated
  const enableCollaboration = !!user;

  const { data: page, isLoading } = useQuery({
    queryKey: ['pages', pageId],
    queryFn: async () => {
      const response = await apiClient.GET('/api/Pages/{id}', {
        params: { path: { id: pageId } },
      });
      if (response.error) throw new Error('Failed to fetch page');
      return response.data;
    },
  });

  // Track collaboration service with state
  const [collaborationService, setCollaborationService] = useState<CollaborationService | null>(null);
  const [isCollaborationReady, setIsCollaborationReady] = useState(false);
  const disconnectPromiseRef = useRef<Promise<void> | null>(null);

  // Subscribe to presence updates via React Query
  const { data: presenceList = [] } = useQuery({
    queryKey: ['presence', pageId],
    queryFn: () => collaborationService?.getPresence() ?? [],
    enabled: !!collaborationService,
    refetchInterval: false, // Only updates via setQueryData
  });

  // Manage collaboration service lifecycle based on pageId
  useEffect(() => {
    // Reset ready state when pageId changes
    setIsCollaborationReady(false);

    // Invalidate cache to ensure fresh data on page navigation
    queryClient.invalidateQueries({ queryKey: ['pages', pageId] });

    if (!enableCollaboration || !user) {
      setCollaborationService(null);
      setIsCollaborationReady(true); // Ready immediately if no collaboration
      return;
    }

    let mounted = true;
    let service: CollaborationService | null = null;

    const setupService = async () => {
      // Wait for any previous disconnect to complete
      if (disconnectPromiseRef.current) {
        try {
          await disconnectPromiseRef.current;
        } catch (err) {
          console.error('Previous disconnect failed:', err);
        }
        disconnectPromiseRef.current = null;
      }

      if (!mounted) return;

      // Create new service
      service = new CollaborationService(
        pageId,
        { id: user.id, name: user.name, color: getUserColor(user.id) },
        queryClient,
        API_BASE_URL
      );

      // Set in state so React can use it
      setCollaborationService(service);

      // Connect
      try {
        await service.connect();
        if (mounted) {
          setIsCollaborationReady(true);
        }
      } catch (error) {
        if (mounted) {
          console.error('Failed to connect collaboration:', error);
          toast.error('Failed to connect to collaboration server');
          setIsCollaborationReady(true); // Allow editor to render anyway
        }
      }
    };

    setupService();

    return () => {
      mounted = false;
      if (service) {
        // Store disconnect promise to ensure it completes
        disconnectPromiseRef.current = service.disconnect().catch(err => {
          console.error('Disconnect failed:', err);
        });
      }
      setCollaborationService(null);
      setIsCollaborationReady(false);
    };
  }, [pageId, enableCollaboration, user, queryClient]);

  // Sync localTitle with page data from React Query (only if not actively editing)
  useLayoutEffect(() => {
    if (page?.title !== undefined && page?.title !== null && !isEditingTitle) {
      setLocalTitle(page.title);
    }
  }, [page?.title, pageId, isEditingTitle]);

  const updateTitleMutation = useUpdatePageTitle(pageId, orgId);

  // Broadcast Channel sync for title updates across tabs/windows
  const { broadcastTitle } = usePageTitleSync(
    pageId,
    useCallback((newTitle: string) => {
      // Update local title when receiving broadcast from sidebar
      setLocalTitle(newTitle);
      // Also update query cache
      queryClient.setQueryData(['pages', pageId], (old: PageDto | undefined) => {
        if (!old) return old;
        return { ...old, title: newTitle };
      });
    }, [pageId, queryClient])
  );

  const handleTitleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newTitle = e.target.value;
    setLocalTitle(newTitle);
  };

  const handleTitleBlur = () => {
    setIsEditingTitle(false);
    if (localTitle !== page?.title && localTitle.trim()) {
      updateTitleMutation.mutate({
        newTitle: localTitle,
        currentPageId: pageId,
      });
      // Broadcast to other contexts (e.g., sidebar)
      broadcastTitle(localTitle);
    }
  };

  const handleTitleFocus = () => {
    setIsEditingTitle(true);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
      </div>
    );
  }

  if (!page) {
    return (
      <div className="flex items-center justify-center h-screen">
        <p className="text-gray-500">Page not found</p>
      </div>
    );
  }

  // Wait for collaboration service to be ready before rendering editor
  if (!isCollaborationReady) {
    return (
      <div className="w-full h-screen overflow-y-auto bg-white dark:bg-gray-950">
        <div className="max-w-4xl mx-auto pt-16">
          <input
            type="text"
            value={localTitle}
            onChange={handleTitleChange}
            onFocus={handleTitleFocus}
            onBlur={handleTitleBlur}
            className="w-full px-16 text-5xl font-bold border-none outline-none bg-transparent placeholder-gray-300 dark:placeholder-gray-700 dark:text-gray-100"
            placeholder="Untitled"
          />
          <div className="flex items-center justify-center h-64">
            <div className="flex flex-col items-center gap-2">
              <Loader2 className="h-6 w-6 animate-spin text-gray-400" />
              <p className="text-sm text-gray-500">Connecting to collaboration server...</p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-screen overflow-y-auto bg-white dark:bg-gray-950">
      <div className="max-w-4xl mx-auto pt-16">
        {collaborationService && presenceList.length > 0 && (
          <div className="fixed top-4 right-4 bg-gray-100 dark:bg-gray-800 px-3 py-1 rounded-md text-sm text-gray-600 dark:text-gray-400 flex items-center gap-2">
            <Users className="h-3 w-3" />
            {presenceList.length} other{presenceList.length !== 1 ? 's' : ''} online
          </div>
        )}
        <input
          type="text"
          value={localTitle}
          onChange={handleTitleChange}
          onFocus={handleTitleFocus}
          onBlur={handleTitleBlur}
          className="w-full px-16 text-5xl font-bold border-none outline-none bg-transparent placeholder-gray-300 dark:placeholder-gray-700 dark:text-gray-100"
          placeholder="Untitled"
        />
        <BlockEditor
          key={pageId}
          content={page?.blocks?.[0]?.json || ''}
          placeholder="Type '/' for commands..."
          collaborationService={collaborationService || undefined}
          user={user ? { id: user.id, name: user.name, color: getUserColor(user.id) } : undefined}
          pageId={pageId}
          orgId={orgId}
        />
      </div>
    </div>
  );
}
