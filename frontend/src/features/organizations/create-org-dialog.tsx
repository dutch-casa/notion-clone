import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { apiClient } from '@/lib/api/client';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';

interface CreateOrgDialogProps {
  trigger?: React.ReactNode;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
}

export function CreateOrgDialog({ trigger, open: controlledOpen, onOpenChange }: CreateOrgDialogProps) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [internalOpen, setInternalOpen] = useState(false);
  const [orgName, setOrgName] = useState('');

  const isOpen = controlledOpen !== undefined ? controlledOpen : internalOpen;
  const setIsOpen = onOpenChange || setInternalOpen;

  // Create organization mutation
  const createOrgMutation = useMutation({
    mutationFn: async (name: string) => {
      const response = await apiClient.POST('/api/Organizations', {
        body: { name },
      });
      if (response.error) {
        throw new Error('Failed to create organization');
      }
      return response.data;
    },
    onSuccess: (org) => {
      queryClient.invalidateQueries({ queryKey: ['organizations'] });
      toast.success('Organization created successfully');
      setIsOpen(false);
      setOrgName('');
      if (org?.orgId) {
        navigate({ to: '/organizations/$orgId', params: { orgId: org.orgId } });
      }
    },
    onError: () => {
      toast.error('Failed to create organization');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (orgName.trim()) {
      createOrgMutation.mutate(orgName);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      {trigger && (
        <DialogTrigger asChild>
          {trigger}
        </DialogTrigger>
      )}
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Create new organization</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4 pt-4">
          <div className="space-y-2">
            <label htmlFor="org-name" className="text-sm font-medium text-gray-700">
              Organization name
            </label>
            <Input
              id="org-name"
              placeholder="Acme Inc."
              value={orgName}
              onChange={(e) => setOrgName(e.target.value)}
              className="w-full"
              autoFocus
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="submit"
              disabled={!orgName.trim() || createOrgMutation.isPending}
              className="bg-blue-600 hover:bg-blue-700"
            >
              {createOrgMutation.isPending ? 'Creating...' : 'Create organization'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
