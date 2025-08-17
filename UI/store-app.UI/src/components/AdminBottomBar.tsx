import { NavLink, useLocation } from 'react-router-dom';
import { LayoutDashboard, ShoppingCart, PackageSearch, UsersRound } from 'lucide-react';

const adminNav = [
  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/admin/orders', label: 'Orders', icon: ShoppingCart },
  { to: '/admin/products', label: 'Products', icon: PackageSearch },
  { to: '/admin/users', label: 'Users', icon: UsersRound },
];

const AdminBottomBar = () => {
  const location = useLocation();
  return (
    <nav className="fixed bottom-0 left-0 right-0 z-50 flex bg-background border-t shadow-md h-16 md:hidden">
      {adminNav.map((item) => {
        const isActive = location.pathname === item.to || (item.to !== '/admin' && location.pathname.startsWith(item.to));
        const Icon = item.icon;
        return (
          <NavLink
            key={item.to}
            to={item.to}
            className={`flex flex-1 flex-col items-center justify-center gap-1 text-xs transition-colors ${isActive ? 'text-primary' : 'text-muted-foreground hover:text-primary'}`}
          >
            <Icon className="w-6 h-6 mb-0.5" />
            {item.label}
          </NavLink>
        );
      })}
    </nav>
  );
};

export default AdminBottomBar;
