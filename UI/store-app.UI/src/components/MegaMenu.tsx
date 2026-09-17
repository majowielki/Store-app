import { NavLink } from 'react-router-dom';
import { useGetProductsMetaQuery } from '@/api/catalog';

const MegaMenu = () => {
  const { data: meta } = useGetProductsMetaQuery();
  const categories = meta?.categories ?? [];

  if (categories.length === 0) return null;

  return (
    <div className="hidden lg:block border-t border-b">
      <div className="align-element py-3">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {categories.map((c) => (
            <NavLink key={c} to={`/products?category=${encodeURIComponent(c)}`} className="text-sm hover:text-primary">
              {c}
            </NavLink>
          ))}
        </div>
      </div>
    </div>
  );
};

export default MegaMenu;
