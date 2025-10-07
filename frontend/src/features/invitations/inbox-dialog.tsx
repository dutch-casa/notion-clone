import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api/client';
import { toast } from 'sonner';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { MailOpen, Check, X } from 'lucide-react';

interface InboxDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function InboxDialog({ open, onOpenChange }: InboxDialogProps) {
  const queryClient = useQueryClient();

  // Fetch invitations
  const { data: invitations = [] } = useQuery({
    queryKey: ['invitations'],
    queryFn: async () => {
      const response = await apiClient.GET('/api/Organizations/invitations');
      if (response.error) throw new Error('Failed to fetch invitations');
      return (response.data as any) || [];
    },
    enabled: open,
  });

  // Accept invitation mutation
  const acceptMutation = useMutation({
    mutationFn: async (invitationId: string) => {
      const response = await apiClient.POST('/api/Organizations/invitations/{invitationId}/accept', {
        params: { path: { invitationId } },
      });
      if (response.error) throw new Error('Failed to accept invitation');
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invitations'] });
      queryClient.invalidateQueries({ queryKey: ['organizations'] });
      toast.success('Invitation accepted');
    },
    onError: () => {
      toast.error('Failed to accept invitation');
    },
  });

  // Decline invitation mutation
  const declineMutation = useMutation({
    mutationFn: async (invitationId: string) => {
      const response = await apiClient.POST('/api/Organizations/invitations/{invitationId}/decline', {
        params: { path: { invitationId } },
      });
      if (response.error) throw new Error('Failed to decline invitation');
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invitations'] });
      toast.success('Invitation declined');
    },
    onError: () => {
      toast.error('Failed to decline invitation');
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <MailOpen className="h-5 w-5" />
            Invitations
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-3 max-h-[60vh] overflow-y-auto">
          {(invitations as any[]).length === 0 ? (
            <div className="text-center py-8 text-gray-500 dark:text-gray-400">
              No pending invitations
            </div>
          ) : (
            (invitations as any[]).map((invitation: any) => (
              <div
                key={invitation.invitationId}
                className="flex items-center justify-between p-4 border rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
              >
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <h3 className="font-medium text-gray-900 dark:text-gray-100">
                      {invitation.orgName}
                    </h3>
                    <Badge variant="secondary">{invitation.role}</Badge>
                  </div>
                  <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                    Invited by {invitation.inviterName}
                  </p>
                  <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
                    {new Date(invitation.createdAt).toLocaleDateString()}
                  </p>
                </div>

                <div className="flex gap-2 ml-4">
                  <Button
                    size="sm"
                    onClick={() => acceptMutation.mutate(invitation.invitationId)}
                    disabled={acceptMutation.isPending || declineMutation.isPending}
                  >
                    <Check className="h-4 w-4 mr-1" />
                    Accept
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => declineMutation.mutate(invitation.invitationId)}
                    disabled={acceptMutation.isPending || declineMutation.isPending}
                  >
                    <X className="h-4 w-4 mr-1" />
                    Decline
                  </Button>
                </div>
              </div>
            ))
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
