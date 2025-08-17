import { categories } from "@/utils/categories";
import { NavLink, useNavigate } from "react-router-dom";
import { House, X } from "lucide-react";
import React from "react";

interface CustomSidebarProps {
  open: boolean;
  setOpen: (v: boolean) => void;
}

const CustomSidebar: React.FC<CustomSidebarProps> = ({ open, setOpen }) => {
  React.useEffect(() => {
    if (!open) return;
    const onEsc = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    document.addEventListener("keydown", onEsc);
    return () => document.removeEventListener("keydown", onEsc);
  }, [open, setOpen]);


  const navigate = useNavigate();
  if (!open) return null;

  // Scroll to top of homepage when Home is clicked
  const handleHomeClick = () => {
    setOpen(false);
    navigate('/');
    setTimeout(() => {
      if (window.location.pathname === '/') {
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    }, 100);
  };

  return (
    <>
      {/* Overlay */}
      <div
        className="fixed inset-0 bg-black/50 z-[9998]"
        onClick={() => setOpen(false)}
        aria-label="Close sidebar overlay"
      />
      {/* Sidebar */}
      <aside
        className="fixed top-0 left-0 h-full w-64 bg-background z-[9999] shadow-lg transition-transform duration-300 ease-in-out"
        style={{ transform: open ? "translateX(0)" : "translateX(-100%)" }}
        aria-label="Sidebar navigation"
      >
        <button
          className="absolute top-4 right-4 text-muted-foreground hover:text-foreground"
          onClick={() => setOpen(false)}
          aria-label="Close sidebar"
        >
          <X className="w-6 h-6" />
        </button>
        <div className="flex flex-col gap-2 p-6">
          <button
            className="flex items-center gap-2 font-bold text-lg mb-4 text-left"
            onClick={handleHomeClick}
            style={{ width: '100%' }}
          >
            <House className="w-6 h-6" /> Home
          </button>
          <NavLink
            to="/products?group=all"
            className="block py-2 px-2 rounded hover:bg-accent font-medium text-foreground"
            onClick={() => setOpen(false)}
          >
            All products
          </NavLink>
          {categories.map((c) => (
            <NavLink
              key={c.slug}
              to={c.group === "sale" ? "/products?sale=on" : `/products?group=${encodeURIComponent(c.group)}`}
              className="block py-2 px-2 rounded hover:bg-accent font-medium text-foreground"
              onClick={() => setOpen(false)}
            >
              {c.label}
            </NavLink>
          ))}
        </div>
      </aside>
    </>
  );
};

export default CustomSidebar;
