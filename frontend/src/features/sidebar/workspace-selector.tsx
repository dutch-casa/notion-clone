import { useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { Check, ChevronsUpDown, Plus } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from '@/components/ui/command';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { CreateOrgDialog } from '@/features/organizations/create-org-dialog';

interface Organization {
  id?: string;
  name?: string | null;
}

interface WorkspaceSelectorProps {
  organizations: Organization[];
  currentOrg?: Organization;
}

export function WorkspaceSelector({ organizations, currentOrg }: WorkspaceSelectorProps) {
  const [open, setOpen] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const navigate = useNavigate();

  return (
    <>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="ghost"
            role="combobox"
            aria-expanded={open}
            className="w-full justify-between px-2 py-1 h-auto font-semibold hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            <span className="truncate">{currentOrg?.name || 'Select workspace'}</span>
            <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-[240px] p-0" align="start">
          <Command>
            <CommandInput placeholder="Search workspace..." />
            <CommandList>
              <CommandEmpty>No workspace found.</CommandEmpty>
              <CommandGroup>
                <CommandItem
                  onSelect={() => {
                    setOpen(false);
                    setDialogOpen(true);
                  }}
                  className="cursor-pointer"
                >
                  <Plus className="mr-2 h-4 w-4" />
                  New workspace
                </CommandItem>
              </CommandGroup>
            <CommandSeparator />
            <CommandGroup heading="Workspaces">
              {organizations.map((org) => (
                <CommandItem
                  key={org.id}
                  value={org.name || ''}
                  onSelect={() => {
                    navigate({ to: '/organizations/$orgId', params: { orgId: org.id! } });
                    setOpen(false);
                  }}
                  className="cursor-pointer"
                >
                  <Check
                    className={cn(
                      'mr-2 h-4 w-4',
                      currentOrg?.id === org.id ? 'opacity-100' : 'opacity-0'
                    )}
                  />
                  {org.name}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
    <CreateOrgDialog open={dialogOpen} onOpenChange={setDialogOpen} />
    </>
  );
}
