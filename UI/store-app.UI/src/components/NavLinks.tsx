import { links } from '@/utils';
import { NavLink } from 'react-router-dom';
// import { useAppSelector } from '@/hooks';

const NavLinks = () => {
  // user selector removed (was unused)
  const hiddenInHeader = new Set(['/', 'about', 'products', 'cart', 'checkout', 'orders']);
  return (
    <div className='hidden lg:flex justify-center items-center gap-x-4'>
      {links
        .filter((link) => !hiddenInHeader.has(link.href))
        .map((link) => {
  // all restricted routes are hidden in header now
        return (
          <NavLink
            to={link.href}
            key={link.label}
            className={({ isActive }) => {
              return `capitalize font-light tracking-wide ${
                isActive ? 'text-primary' : ''
              }`;
            }}
          >
            {link.label}
          </NavLink>
        );
      })}
  {/* Admin link removed; now in account dropdown */}
    </div>
  );
}
export default NavLinks;
