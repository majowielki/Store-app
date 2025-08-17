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
import { LayoutDashboard, PackageSearch, UsersRound, ShoppingCart } from 'lucide-react';
import AdminHeader from '@/components/AdminHeader';
import AdminBottomBar from '@/components/AdminBottomBar';
import { useIsMobile } from '@/hooks/use-mobile';

const AdminLayout = () => {
  const nav = [
    { to: '/admin', label: 'Dashboard', icon: <LayoutDashboard /> },
    { to: '/admin/orders', label: 'Orders', icon: <ShoppingCart /> },
    { to: '/admin/products', label: 'Products', icon: <PackageSearch /> },
    { to: '/admin/users', label: 'Users', icon: <UsersRound /> },
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
          </SidebarContent>
          <SidebarFooter>
            <Card className="p-2 text-xs">Use Ctrl+B to toggle</Card>
          </SidebarFooter>
          <SidebarRail />
        </Sidebar>
        <SidebarInset>
          <AdminHeader />
          <main className="p-4">
            <Outlet />
          </main>
          {isMobile && <AdminBottomBar />}
        </SidebarInset>
      </div>
    </SidebarProvider>
  );
};

export default AdminLayout;
