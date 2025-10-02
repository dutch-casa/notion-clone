import { type ReactNode, createContext, useContext, useState, useEffect, useLayoutEffect, useRef } from 'react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from '@/components/ui/context-menu';
import { ChevronLeft, ChevronRight, Edit2, Trash2 } from 'lucide-react';
import { cn } from '@/lib/utils';

// Sidebar Context
interface SidebarContextType {
  isCollapsed: boolean;
  toggleSidebar: () => void;
}

const SidebarContext = createContext<SidebarContextType | null>(null);

const useSidebarContext = () => {
  const context = useContext(SidebarContext);
  if (!context) throw new Error('Sidebar components must be used within Sidebar.Provider');
  return context;
};

// Sidebar Provider
export function SidebarProvider({ children }: { children: ReactNode }) {
  const [isCollapsed, setIsCollapsed] = useState(false);

  const toggleSidebar = () => {
    setIsCollapsed(prev => !prev);
  };

  // Keyboard shortcut: Cmd/Ctrl + \
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === '\\') {
        e.preventDefault();
        toggleSidebar();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  return (
    <SidebarContext.Provider value={{ isCollapsed, toggleSidebar }}>
      {children}
    </SidebarContext.Provider>
  );
}

// Sidebar Container
export function SidebarContainer({ children }: { children: ReactNode }) {
  const { isCollapsed } = useSidebarContext();

  return (
    <aside
      className={cn(
        'relative flex flex-col h-screen bg-gray-50 dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 transition-all duration-200 ease-in-out',
        isCollapsed ? 'w-0' : 'w-64'
      )}
    >
      <div className={cn('flex flex-col h-full', isCollapsed && 'opacity-0 pointer-events-none')}>
        {children}
      </div>
    </aside>
  );
}

// Sidebar Header
export function SidebarHeader({ children }: { children: ReactNode }) {
  return (
    <div className="flex items-center justify-between px-3 py-2 min-h-[52px]">
      {children}
    </div>
  );
}

// Sidebar Toggle Button
export function SidebarToggle() {
  const { isCollapsed, toggleSidebar } = useSidebarContext();

  return (
    <Button
      variant="ghost"
      size="sm"
      onClick={toggleSidebar}
      className="h-7 w-7 p-0 hover:bg-gray-200 dark:hover:bg-gray-800"
      title={isCollapsed ? 'Expand sidebar (Cmd/Ctrl + \\)' : 'Collapse sidebar (Cmd/Ctrl + \\)'}
    >
      {isCollapsed ? (
        <ChevronRight className="h-4 w-4" />
      ) : (
        <ChevronLeft className="h-4 w-4" />
      )}
    </Button>
  );
}

// Sidebar Content (scrollable area)
export function SidebarContent({ children }: { children: ReactNode }) {
  return (
    <div className="flex-1 overflow-y-auto px-2 py-2 space-y-1">
      {children}
    </div>
  );
}

// Sidebar Section
export function SidebarSection({ children, title }: { children: ReactNode; title?: string }) {
  return (
    <div className="space-y-1">
      {title && (
        <div className="px-2 py-1 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
          {title}
        </div>
      )}
      {children}
    </div>
  );
}

// Sidebar Item
interface SidebarItemProps {
  children: ReactNode;
  icon?: ReactNode;
  active?: boolean;
  onClick?: () => void;
  className?: string;
}

export function SidebarItem({ children, icon, active, onClick, className }: SidebarItemProps) {
  return (
    <button
      onClick={onClick}
      className={cn(
        'w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded-md transition-colors',
        'hover:bg-gray-200 dark:hover:bg-gray-800',
        active ? 'bg-gray-200 dark:bg-gray-800 text-gray-900 dark:text-gray-100' : 'text-gray-700 dark:text-gray-300',
        className
      )}
    >
      {icon && <span className="flex-shrink-0">{icon}</span>}
      <span className="flex-1 text-left truncate">{children}</span>
    </button>
  );
}

// Sidebar Editable Item
interface SidebarEditableItemProps {
  value: string;
  icon?: ReactNode;
  active?: boolean;
  onClick?: () => void;
  onSave?: (value: string) => void;
  onDelete?: () => void;
  canDelete?: boolean;
  className?: string;
}

export function SidebarEditableItem({
  value,
  icon,
  active,
  onClick,
  onSave,
  onDelete,
  canDelete = true,
  className
}: SidebarEditableItemProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState(value);
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useLayoutEffect(() => {
    if (isEditing && inputRef.current) {
      inputRef.current.focus();
      inputRef.current.select();
    }
  }, [isEditing]);

  const handleSave = () => {
    if (editValue.trim() && editValue !== value) {
      onSave?.(editValue.trim());
    } else {
      setEditValue(value);
    }
    setIsEditing(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      if (isEditing) {
        e.preventDefault();
        handleSave();
      } else {
        e.preventDefault();
        setIsEditing(true);
      }
    } else if (e.key === 'Escape') {
      setEditValue(value);
      setIsEditing(false);
    }
  };

  const handleBlur = () => {
    handleSave();
  };

  const handleDoubleClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    setIsEditing(true);
  };

  const handleRename = () => {
    // Delay to ensure context menu fully closes before focusing
    setTimeout(() => {
      setIsEditing(true);
    }, 10);
  };

  const handleDelete = () => {
    onDelete?.();
  };

  const handleContainerClick = (e: React.MouseEvent) => {
    // Only trigger onClick if not editing and clicking on the container (not input)
    if (!isEditing && e.target === containerRef.current) {
      onClick?.();
    }
  };

  return (
    <ContextMenu>
      <ContextMenuTrigger asChild>
        <div
          ref={containerRef}
          onClick={handleContainerClick}
          onDoubleClick={handleDoubleClick}
          className={cn(
            'w-full flex items-center gap-2 px-2 py-1.5 text-sm rounded-md transition-colors relative cursor-pointer',
            isEditing ? 'bg-gray-200 dark:bg-gray-800' : 'hover:bg-gray-200 dark:hover:bg-gray-800',
            active && !isEditing ? 'bg-gray-200 dark:bg-gray-800 text-gray-900 dark:text-gray-100' : 'text-gray-700 dark:text-gray-300',
            className
          )}
        >
          {icon && <span className="flex-shrink-0">{icon}</span>}

          <button
            onClick={onClick}
            onKeyDown={handleKeyDown}
            className={cn(
              'flex-1 text-left truncate bg-transparent border-none outline-none cursor-pointer',
              isEditing && 'sr-only'
            )}
            tabIndex={isEditing ? -1 : 0}
          >
            {value}
          </button>

          <input
            ref={inputRef}
            type="text"
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
            onKeyDown={handleKeyDown}
            onBlur={handleBlur}
            onClick={(e) => e.stopPropagation()}
            className={cn(
              'flex-1 bg-transparent outline-none text-gray-900 dark:text-gray-100 absolute left-0 right-0 px-2 py-1.5',
              !isEditing && 'sr-only',
              icon && 'pl-8'
            )}
            tabIndex={isEditing ? 0 : -1}
            style={{ marginLeft: icon ? '1.75rem' : '0' }}
          />
        </div>
      </ContextMenuTrigger>
      <ContextMenuContent className="w-48">
        <ContextMenuItem onSelect={handleRename}>
          <Edit2 className="h-4 w-4 mr-2" />
          Rename
        </ContextMenuItem>
        {canDelete && onDelete && (
          <ContextMenuItem onSelect={handleDelete} className="text-red-600 dark:text-red-400">
            <Trash2 className="h-4 w-4 mr-2" />
            Delete
          </ContextMenuItem>
        )}
      </ContextMenuContent>
    </ContextMenu>
  );
}

// Sidebar Separator
export function SidebarSeparator() {
  return <Separator className="my-2" />;
}

// Sidebar Footer
export function SidebarFooter({ children }: { children: ReactNode }) {
  return (
    <div className="border-t border-gray-200 dark:border-gray-800 px-3 py-2">
      {children}
    </div>
  );
}

// Main Layout with Sidebar
export function SidebarLayout({ children, sidebar }: { children: ReactNode; sidebar: ReactNode }) {
  const { isCollapsed } = useSidebarContext();

  return (
    <div className="flex h-screen overflow-hidden">
      {sidebar}
      <main className="flex-1 overflow-auto">
        {children}
      </main>
      {isCollapsed && (
        <div className="fixed left-0 top-0 h-full flex items-start pt-2 pl-2 z-50">
          <SidebarToggle />
        </div>
      )}
    </div>
  );
}
