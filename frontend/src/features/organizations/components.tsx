import { type ReactNode, type FormEvent, createContext, useContext, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Plus, Users, Crown, User, Mail } from 'lucide-react';
import { cn } from '@/lib/utils';

// Organization List Context
interface OrgListContextType {
  onSelectOrg?: (orgId: string) => void;
}

const OrgListContext = createContext<OrgListContextType | null>(null);

const useOrgListContext = () => {
  const context = useContext(OrgListContext);
  if (!context) throw new Error('OrgList components must be used within OrgList.Provider');
  return context;
};

// Organization List Components
export function OrgListProvider({ children, onSelectOrg }: { children: ReactNode; onSelectOrg?: (orgId: string) => void }) {
  return (
    <OrgListContext.Provider value={{ onSelectOrg }}>
      <div className="flex flex-col h-full">
        {children}
      </div>
    </OrgListContext.Provider>
  );
}

export function OrgListHeader({ children }: { children: ReactNode }) {
  return (
    <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
      {children}
    </div>
  );
}

export function OrgListTitle({ children }: { children: ReactNode }) {
  return <h1 className="text-2xl font-semibold text-gray-900">{children}</h1>;
}

export function OrgListActions({ children }: { children: ReactNode }) {
  return <div className="flex items-center gap-2">{children}</div>;
}

export function OrgListContent({ children }: { children: ReactNode }) {
  return (
    <div className="flex-1 overflow-y-auto px-6 py-4">
      {children}
    </div>
  );
}

export function OrgListGrid({ children }: { children: ReactNode }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      {children}
    </div>
  );
}

export function OrgListEmpty({ children }: { children: ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      <Users className="w-12 h-12 text-gray-300 mb-4" />
      <div className="text-gray-500">{children}</div>
    </div>
  );
}

interface OrgCardProps {
  id: string;
  name: string | null | undefined;
  role: string | null | undefined;
  memberCount?: number;
  className?: string;
}

export function OrgCard({ id, name, role, memberCount, className }: OrgCardProps) {
  const { onSelectOrg } = useOrgListContext();

  const isOwner = role === 'owner';

  return (
    <button
      onClick={() => onSelectOrg?.(id)}
      className={cn(
        'group relative flex flex-col gap-3 p-5 rounded-lg border border-gray-200',
        'hover:border-gray-300 hover:shadow-sm transition-all',
        'bg-white text-left',
        className
      )}
    >
      {/* Org Icon and Name */}
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0 w-10 h-10 rounded bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-semibold text-lg">
          {name?.charAt(0).toUpperCase() ?? 'O'}
        </div>
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-gray-900 truncate">{name}</h3>
          <div className="flex items-center gap-2 mt-1">
            {isOwner ? (
              <span className="inline-flex items-center gap-1 text-xs text-amber-700">
                <Crown className="w-3 h-3" />
                Owner
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 text-xs text-gray-500">
                <User className="w-3 h-3" />
                {role}
              </span>
            )}
            {memberCount !== undefined && (
              <>
                <span className="text-gray-300">·</span>
                <span className="text-xs text-gray-500">{memberCount} members</span>
              </>
            )}
          </div>
        </div>
      </div>
    </button>
  );
}

// Create Organization Dialog Context
interface CreateOrgContextType {
  name: string;
  setName: (name: string) => void;
  isSubmitting: boolean;
  isOpen: boolean;
  setIsOpen: (open: boolean) => void;
  onSubmit: () => Promise<void>;
}

const CreateOrgContext = createContext<CreateOrgContextType | null>(null);

const useCreateOrgContext = () => {
  const context = useContext(CreateOrgContext);
  if (!context) throw new Error('CreateOrg components must be used within CreateOrg.Provider');
  return context;
};

export function CreateOrgProvider({
  children,
  onSubmit
}: {
  children: ReactNode;
  onSubmit: (name: string) => Promise<void>;
}) {
  const [name, setName] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isOpen, setIsOpen] = useState(false);

  const handleSubmit = async () => {
    if (!name.trim()) return;
    setIsSubmitting(true);
    try {
      await onSubmit(name);
      setName(''); // Reset on success
      setIsOpen(false); // Close dialog
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <CreateOrgContext.Provider value={{ name, setName, isSubmitting, isOpen, setIsOpen, onSubmit: handleSubmit }}>
      {children}
    </CreateOrgContext.Provider>
  );
}

export function CreateOrgTrigger({ children }: { children: ReactNode }) {
  const { isOpen, setIsOpen } = useCreateOrgContext();

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        {children}
      </DialogTrigger>
      <CreateOrgDialog />
    </Dialog>
  );
}

function CreateOrgDialog() {
  const { name, setName, isSubmitting, onSubmit } = useCreateOrgContext();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    await onSubmit();
  };

  return (
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
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="w-full"
            autoFocus
          />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="submit"
            disabled={!name.trim() || isSubmitting}
            className="bg-blue-600 hover:bg-blue-700"
          >
            {isSubmitting ? 'Creating...' : 'Create organization'}
          </Button>
        </div>
      </form>
    </DialogContent>
  );
}

export function CreateOrgButton() {
  return (
    <Button variant="outline" className="gap-2">
      <Plus className="w-4 h-4" />
      New organization
    </Button>
  );
}

// Organization Settings Context
interface OrgSettingsContextType {
  orgId: string;
  orgName: string | null | undefined;
  members: Array<{
    userId: string;
    userName: string;
    userEmail: string;
    role: string | null | undefined;
    joinedAt: string;
  }> | null | undefined;
  currentUserRole: string | null | undefined;
  onInviteMember?: (userId: string, role: string) => Promise<void>;
  onRemoveMember?: (userId: string) => Promise<void>;
}

const OrgSettingsContext = createContext<OrgSettingsContextType | null>(null);

const useOrgSettingsContext = () => {
  const context = useContext(OrgSettingsContext);
  if (!context) throw new Error('OrgSettings components must be used within OrgSettings.Provider');
  return context;
};

export function OrgSettingsProvider({
  children,
  orgId,
  orgName,
  members,
  currentUserRole,
  onInviteMember,
  onRemoveMember
}: {
  children: ReactNode;
  orgId: string;
  orgName: string | null | undefined;
  members: Array<{
    userId: string;
    userName: string;
    userEmail: string;
    role: string | null | undefined;
    joinedAt: string;
  }> | null | undefined;
  currentUserRole: string | null | undefined;
  onInviteMember?: (userId: string, role: string) => Promise<void>;
  onRemoveMember?: (userId: string) => Promise<void>;
}) {
  return (
    <OrgSettingsContext.Provider value={{ orgId, orgName, members, currentUserRole, onInviteMember, onRemoveMember }}>
      <div className="flex flex-col h-screen max-w-4xl mx-auto">
        {children}
      </div>
    </OrgSettingsContext.Provider>
  );
}

export function OrgSettingsHeader({ children }: { children: ReactNode }) {
  return (
    <div className="px-6 py-4 border-b border-gray-200">
      {children}
    </div>
  );
}

export function OrgSettingsTitle() {
  const { orgName } = useOrgSettingsContext();
  return <h1 className="text-2xl font-semibold text-gray-900">{orgName}</h1>;
}

export function OrgSettingsContent({ children }: { children: ReactNode }) {
  return (
    <div className="flex-1 overflow-y-auto px-6 py-6 space-y-8">
      {children}
    </div>
  );
}

export function OrgSettingsSection({ title, description, children }: {
  title: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-gray-900">{title}</h2>
        {description && <p className="text-sm text-gray-500 mt-1">{description}</p>}
      </div>
      {children}
    </div>
  );
}

export function OrgMembersList() {
  const { members, currentUserRole, onRemoveMember } = useOrgSettingsContext();
  const canRemove = currentUserRole === 'owner' || currentUserRole === 'admin';

  if (!members || members.length === 0) {
    return (
      <div className="text-sm text-gray-500">No members yet.</div>
    );
  }

  return (
    <div className="space-y-2">
      {members.map((member) => {
        const isOwner = member.role === 'owner';
        const initials = member.userName
          ?.split(' ')
          .map(n => n[0])
          .join('')
          .toUpperCase()
          .slice(0, 2) || 'U';

        return (
          <div
            key={member.userId}
            className="flex items-center justify-between px-4 py-3 rounded-lg border border-gray-200 hover:bg-gray-50"
          >
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-sm font-semibold">
                {initials}
              </div>
              <div>
                <div className="text-sm font-medium text-gray-900">{member.userName}</div>
                <div className="flex items-center gap-1 text-xs text-gray-500 capitalize">
                  {isOwner ? (
                    <>
                      <Crown className="w-3 h-3" />
                      Owner
                    </>
                  ) : (
                    <>
                      <User className="w-3 h-3" />
                      {member.role}
                    </>
                  )}
                </div>
              </div>
            </div>
            {canRemove && !isOwner && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onRemoveMember?.(member.userId)}
                className="text-red-600 hover:text-red-700 hover:bg-red-50"
              >
                Remove
              </Button>
            )}
          </div>
        );
      })}
    </div>
  );
}

export function InviteByEmailForm({ onSubmit }: { onSubmit: (email: string, role: string) => Promise<void> }) {
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('member');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;

    setIsSubmitting(true);
    try {
      await onSubmit(email, role);
      setEmail('');
      setRole('member');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="rounded-lg border border-gray-200 p-4">
      <div className="flex items-start gap-3">
        <Mail className="w-5 h-5 text-gray-400 mt-2" />
        <div className="flex-1 space-y-3">
          <div>
            <label htmlFor="invite-email" className="text-sm font-medium text-gray-700">
              Invite by email
            </label>
            <Input
              id="invite-email"
              type="email"
              placeholder="colleague@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-1"
            />
          </div>
          <div className="flex items-end gap-2">
            <div className="flex-1">
              <label htmlFor="invite-role" className="text-sm font-medium text-gray-700">
                Role
              </label>
              <Select value={role} onValueChange={setRole}>
                <SelectTrigger id="invite-role" className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="member">Member</SelectItem>
                  <SelectItem value="admin">Admin</SelectItem>
                  <SelectItem value="owner">Owner</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <Button
              type="submit"
              disabled={!email.trim() || isSubmitting}
              className="bg-blue-600 hover:bg-blue-700"
            >
              {isSubmitting ? 'Sending...' : 'Send Invitation'}
            </Button>
          </div>
        </div>
      </div>
    </form>
  );
}
