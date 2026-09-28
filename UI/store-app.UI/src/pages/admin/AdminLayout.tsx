import { Outlet, NavLink } from 'react-router-dom';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarRail,
  SidebarSeparator,
  SidebarTrigger,
} from '@/components/ui/sidebar';
import { Card } from '@/components/ui/card';
import { BookOpenText, Images, LayoutDashboard, Layers, MessageSquareText, PackageSearch, Store, TicketPercent, UsersRound, ShoppingCart } from 'lucide-react';
import AdminHeader from '@/components/AdminHeader';
import AdminBottomBar from '@/components/AdminBottomBar';
import { useIsMobile } from '@/hooks/use-mobile';
import { usePageMeta } from '@/seo';

const AdminLayout = () => {
  usePageMeta({ title: 'Admin', noindex: true });
  const nav = [
    { to: '/admin', label: 'Dashboard', icon: <LayoutDashboard /> },
    { to: '/admin/orders', label: 'Orders', icon: <ShoppingCart /> },
    { to: '/admin/discount-codes', label: 'Discount codes', icon: <TicketPercent /> },
    { to: '/admin/products', label: 'Products', icon: <PackageSearch /> },
    { to: '/admin/reviews', label: 'Reviews', icon: <MessageSquareText /> },
    { to: '/admin/users', label: 'Users', icon: <UsersRound /> },
  ];
  // The shop's editorial content, kept by the content service
  const content = [
    { to: '/admin/content/collections', label: 'Collections', icon: <Layers /> },
    { to: '/admin/content/lookbooks', label: 'Lookbooks', icon: <Images /> },
    { to: '/admin/content/articles', label: 'Journal', icon: <BookOpenText /> },
    { to: '/admin/content/makers', label: 'Makers', icon: <Store /> },
  ];
  const isMobile = useIsMobile();

  return (
    <SidebarProvider>
      <div className="flex min-h-screen w-full">
        <Sidebar variant="sidebar" collapsible="icon">
          <SidebarHeader className="flex items-center justify-between">
            <div className="px-2 text-sm font-semibold">Admin</div>
            <SidebarTrigger />
          </SidebarHeader>
          <SidebarSeparator />
          <SidebarContent>
            <SidebarGroup>
              <SidebarGroupLabel>Navigation</SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {nav.map((n) => (
                    <SidebarMenuItem key={n.to}>
                      <SidebarMenuButton asChild isActive={false}>
                        <NavLink to={n.to} className={({ isActive }) => isActive ? 'data-[active=true]' : ''}>
                          {n.icon}
                          <span>{n.label}</span>
                        </NavLink>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
            <SidebarGroup>
              <SidebarGroupLabel>Content</SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {content.map((n) => (
                    <SidebarMenuItem key={n.to}>
                      <SidebarMenuButton asChild isActive={false}>
                        <NavLink to={n.to} className={({ isActive }) => isActive ? 'data-[active=true]' : ''}>
                          {n.icon}
                          <span>{n.label}</span>
                        </NavLink>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          </SidebarContent>
          <SidebarFooter>
            <Card className="p-2 text-xs">Use Ctrl+B to toggle</Card>
          </SidebarFooter>
          <SidebarRail />
        </Sidebar>
        <SidebarInset>
          <AdminHeader />
          <main className="p-4 pb-24 md:pb-4">
            <Outlet />
          </main>
          {isMobile && <AdminBottomBar />}
        </SidebarInset>
      </div>
    </SidebarProvider>
  );
};

export default AdminLayout;
