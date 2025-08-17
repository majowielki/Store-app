import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { AlignLeft } from "lucide-react";
import { Button } from "./ui/button";
import { categories } from "@/utils/categories";
import { NavLink, useLocation, useSearchParams } from "react-router-dom";

const LinksDropdown = () => {
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const onProducts = location.pathname === '/products';
  const currentGroup = onProducts ? (searchParams.get('group') || 'all') : '';
  const saleActive = onProducts && (searchParams.get('sale') === 'on' || searchParams.get('sale') === 'true');
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild className="lg:hidden">
        <Button variant="outline" size="icon">
          <AlignLeft />

          <span className="sr-only">Toggle links</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="w-64 lg:hidden" align="start" sideOffset={25}>
        <DropdownMenuItem>
          <NavLink
            to={`/products?group=${encodeURIComponent('all')}`}
            className={() =>
              `capitalize w-full ${onProducts && currentGroup === 'all' ? 'text-primary' : ''}`
            }
          >
            All products
          </NavLink>
        </DropdownMenuItem>
        {categories.map((c) => (
          <DropdownMenuItem key={c.slug}>
            <NavLink
              to={
                c.group === 'sale'
                  ? `/products?sale=on`
                  : `/products?group=${encodeURIComponent(c.group)}`
              }
              className={() =>
                `capitalize w-full ${
                  c.group === 'sale'
                    ? saleActive
                      ? 'text-primary'
                      : ''
                    : onProducts && currentGroup === c.group
                      ? 'text-primary'
                      : ''
                }`
              }
            >
              {c.label}
            </NavLink>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
export default LinksDropdown;
