import { type ReactNode } from 'react';
import { SidebarProvider, SidebarLayout } from '@/features/sidebar/components';
import { WorkspaceSidebar } from '@/features/sidebar/workspace-sidebar';

export function AppLayout({ children }: { children: ReactNode }) {
  return (
    <SidebarProvider>
      <SidebarLayout sidebar={<WorkspaceSidebar />}>
        {children}
      </SidebarLayout>
    </SidebarProvider>
  );
}
