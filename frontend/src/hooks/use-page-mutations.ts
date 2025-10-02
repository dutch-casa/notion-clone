import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import { toast } from 'sonner';
import type { PageDto, PageSummary } from '@/lib/api/types';

export function useUpdatePageTitle(pageId: string, orgId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ newTitle, currentPageId }: { newTitle: string; currentPageId: string }) => {
      const response = await apiClient.PATCH('/api/Pages/{id}/title', {
        params: { path: { id: currentPageId } },
        body: { title: newTitle },
      });
      if (response.error) throw new Error('Failed to update title');
      return response.data;
    },
    onMutate: async ({ newTitle, currentPageId }) => {
      // Cancel any outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['pages', currentPageId] });
      await queryClient.cancelQueries({ queryKey: ['pages', orgId] });

      // Snapshot the previous values
      const previousPage = queryClient.getQueryData(['pages', currentPageId]);
      const previousPages = queryClient.getQueryData(['pages', orgId]);

      // Optimistically update the page
      queryClient.setQueryData(['pages', currentPageId], (old: PageDto | undefined) => {
        if (!old) return old;
        return { ...old, title: newTitle };
      });

      // Optimistically update the pages list
      queryClient.setQueryData(['pages', orgId], (old: PageSummary[] | undefined) => {
        if (!old) return old;
        return old.map(page =>
          page.id === currentPageId ? { ...page, title: newTitle } : page
        );
      });

      return { previousPage, previousPages };
    },
    onError: (err, variables, context) => {
      // Rollback on error
      if (context?.previousPage) {
        queryClient.setQueryData(['pages', pageId], context.previousPage);
      }
      if (context?.previousPages) {
        queryClient.setQueryData(['pages', orgId], context.previousPages);
      }
      toast.error('Failed to update title');
    },
    onSuccess: () => {
      toast.success('Title updated');
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['pages', pageId] });
      queryClient.invalidateQueries({ queryKey: ['pages', orgId] });
    },
  });
}
