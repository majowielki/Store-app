import {
  SidebarProvider,
  Sidebar,
  SidebarContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuItem,
  SidebarMenuButton,
} from "@/components/ui/sidebar";
import { categories } from "@/utils/categories";
import { NavLink } from "react-router-dom";
import { House } from "lucide-react";

const SidebarNav = ({ open, setOpen }: { open: boolean; setOpen: (v: boolean) => void }) => {
  return (
    <SidebarProvider open={open} onOpenChange={setOpen}>
      <Sidebar collapsible="offcanvas" side="left">
        <SidebarHeader>
          <NavLink to="/" className="flex items-center gap-2 font-bold text-lg mb-2" onClick={() => setOpen(false)}>
            <House className="w-6 h-6" /> Home
          </NavLink>
        </SidebarHeader>
        <SidebarContent>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton asChild>
                <NavLink to="/products?group=all" onClick={() => setOpen(false)}>
                  All products
                </NavLink>
              </SidebarMenuButton>
            </SidebarMenuItem>
            {categories.map((c) => (
              <SidebarMenuItem key={c.slug}>
                <SidebarMenuButton asChild>
                  <NavLink
                    to={c.group === 'sale' ? `/products?sale=on` : `/products?group=${encodeURIComponent(c.group)}`}
                    onClick={() => setOpen(false)}
                  >
                    {c.label}
                  </NavLink>
                </SidebarMenuButton>
              </SidebarMenuItem>
            ))}
          </SidebarMenu>
        </SidebarContent>
      </Sidebar>
      <style>{`.radix-dialog-content, [data-radix-dialog-content], .fixed.z-50[data-sidebar="sidebar"] { z-index: 9999 !important; }`}</style>
    </SidebarProvider>
  );
};

export default SidebarNav;
