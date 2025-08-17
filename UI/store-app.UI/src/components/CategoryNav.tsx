import { categories } from '@/utils/categories';
import { NavLink, useLocation, useSearchParams } from 'react-router-dom';
// import { useAppSelector } from '@/hooks';

const CategoryNav = () => {
  // user selector removed (was unused)
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const onProducts = location.pathname === '/products';
  const currentGroup = onProducts ? (searchParams.get('group') || 'all') : '';
  const saleActive = onProducts && (searchParams.get('sale') === 'on' || searchParams.get('sale') === 'true');
  return (
    <nav className="bg-muted/60 hidden md:block">
      <div className="align-element">
        <ul className="flex flex-wrap items-center gap-4 py-3">
          <li>
            <NavLink
              to={`/products?group=${encodeURIComponent('all')}`}
              className={() =>
                `font-semibold capitalize pb-1 border-b-2 transition-colors ${
                  onProducts && currentGroup === 'all' && !saleActive
                    ? 'border-primary text-foreground'
                    : 'border-transparent hover:border-primary'
                }`
              }
            >
              All products
            </NavLink>
          </li>
          {categories.map((c) => (
            <li key={c.slug}>
              <NavLink
                to={
                  c.group === 'sale'
                    ? `/products?sale=on`
                    : `/products?group=${encodeURIComponent(c.group)}`
                }
                className={() =>
                  `font-semibold capitalize pb-1 border-b-2 transition-colors ${
                    c.group === 'sale'
                      ? saleActive
                        ? 'border-primary text-foreground'
                        : 'border-transparent hover:border-primary'
                      : onProducts && currentGroup === c.group
                      ? 'border-primary text-foreground'
                      : 'border-transparent hover:border-primary'
                  }`
                }
              >
                {c.label}
              </NavLink>
            </li>
          ))}
          {/* Admin button removed; now in account dropdown */}
        </ul>
      </div>
    </nav>
  );
};

export default CategoryNav;
