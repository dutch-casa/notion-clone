import { useState, useEffect } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useTheme } from '@/providers/theme-provider';
import { useAuthStore } from '@/stores/auth-store';
import { apiClient } from '@/lib/api/client';
import { toast } from 'sonner';
import { Monitor, Moon, Sun } from 'lucide-react';
import { cn } from '@/lib/utils';

interface AccountSettingsDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function AccountSettingsDialog({ open, onOpenChange }: AccountSettingsDialogProps) {
  const { user, updateUser } = useAuthStore();
  const { theme, setTheme } = useTheme();
  const [userName, setUserName] = useState(user?.name || '');

  // Reset username when dialog opens
  useEffect(() => {
    if (open && user?.name) {
      setUserName(user.name);
    }
  }, [open, user?.name]);

  const updateNameMutation = useMutation({
    mutationFn: async (name: string) => {
      const response = await apiClient.PUT('/api/Users/{id}', {
        params: { path: { id: user!.id } },
        body: { name },
      });
      if (response.error) {
        throw new Error('Failed to update name');
      }
      return response.data;
    },
    onSuccess: () => {
      updateUser({ name: userName });
      toast.success('Name updated successfully');
    },
    onError: () => {
      toast.error('Failed to update name');
    },
  });

  const handleSave = () => {
    if (userName.trim() && userName !== user?.name) {
      updateNameMutation.mutate(userName);
    }
  };

  const themeOptions = [
    { value: 'light', label: 'Light', icon: Sun },
    { value: 'dark', label: 'Dark', icon: Moon },
    { value: 'system', label: 'System', icon: Monitor },
  ] as const;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl h-[500px] p-0">
        <div className="flex h-full">
          {/* Sidebar with tabs */}
          <Tabs defaultValue="appearance" className="flex h-full w-full" orientation="vertical">
            <div className="w-48 border-r border-gray-200 p-4">
              <DialogHeader className="mb-4">
                <DialogTitle>Account Settings</DialogTitle>
              </DialogHeader>
              <TabsList className="flex flex-col h-auto space-y-1 bg-transparent p-0">
                <TabsTrigger
                  value="appearance"
                  className="w-full justify-start rounded-md data-[state=active]:bg-gray-100"
                >
                  Appearance
                </TabsTrigger>
                <TabsTrigger
                  value="general"
                  className="w-full justify-start rounded-md data-[state=active]:bg-gray-100"
                >
                  General
                </TabsTrigger>
              </TabsList>
            </div>

            {/* Content area */}
            <div className="flex-1 p-6 overflow-y-auto">
              <TabsContent value="appearance" className="mt-0">
                <h3 className="text-lg font-semibold mb-4">Appearance</h3>
                <div className="space-y-4">
                  <div>
                    <Label className="text-sm font-medium mb-3 block">Theme</Label>
                    <div className="grid grid-cols-3 gap-3">
                      {themeOptions.map((option) => {
                        const Icon = option.icon;
                        return (
                          <button
                            key={option.value}
                            onClick={() => setTheme(option.value)}
                            className={cn(
                              'flex flex-col items-center gap-2 p-4 rounded-lg border-[0.25px] transition-all',
                              theme === option.value
                                ? 'border-blue-600 bg-blue-50'
                                : 'border-gray-200 hover:border-gray-300'
                            )}
                          >
                            <Icon className="h-5 w-5" />
                            <span className="text-sm font-medium">{option.label}</span>
                          </button>
                        );
                      })}
                    </div>
                  </div>
                </div>
              </TabsContent>

              <TabsContent value="general" className="mt-0">
                <h3 className="text-lg font-semibold mb-4">General</h3>
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="user-name" className="text-sm font-medium">
                      Name
                    </Label>
                    <Input
                      id="user-name"
                      value={userName}
                      onChange={(e) => setUserName(e.target.value)}
                      className="max-w-md"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="user-email" className="text-sm font-medium">
                      Email
                    </Label>
                    <Input
                      id="user-email"
                      value={user?.email || ''}
                      disabled
                      className="max-w-md"
                    />
                    <p className="text-xs text-gray-500">Email cannot be changed</p>
                  </div>
                  <div className="pt-4">
                    <Button
                      className="bg-blue-600 hover:bg-blue-700"
                      onClick={handleSave}
                      disabled={updateNameMutation.isPending || !userName.trim() || userName === user?.name}
                    >
                      {updateNameMutation.isPending ? 'Saving...' : 'Save changes'}
                    </Button>
                  </div>
                </div>
              </TabsContent>
            </div>
          </Tabs>
        </div>
      </DialogContent>
    </Dialog>
  );
}
