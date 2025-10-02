import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import { toast } from 'sonner';

export function useRemoveMember(orgId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (userId: string) => {
      const response = await apiClient.DELETE('/api/Organizations/{id}/members/{userId}', {
        params: { path: { id: orgId, userId } },
      });
      if (response.error) {
        throw new Error('Failed to remove member');
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['organization', orgId] });
      toast.success('Member removed successfully');
    },
    onError: () => {
      toast.error('Failed to remove member');
    },
  });
}

export function useInviteByEmail(orgId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ email, role }: { email: string; role: string }) => {
      const response = await apiClient.POST('/api/Organizations/{id}/invitations/by-email', {
        params: { path: { id: orgId } },
        body: { email, role },
      });
      if (response.error) {
        throw new Error(response.error?.detail || 'Failed to send invitation');
      }
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['organization', orgId] });
      toast.success('Invitation sent successfully');
    },
    onError: (error: Error) => {
      toast.error(error.message);
    },
  });
}
