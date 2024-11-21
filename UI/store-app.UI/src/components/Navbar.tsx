import LinksDropdown from "./LinksDropdown";
import Logo from "./Logo";
import ModeToggle from "./ModeToggle";
import NavLinks from "./NavLinks";
import CartButton from "./CartButton";

function Navbar() {
  return (
    <nav className="bg-muted py-4">
      <div className="flex align-element justify-between items-center">
        <Logo />
        <LinksDropdown />
        <NavLinks />
        <div className="flex justify-center items-center gap-x-4">
          <ModeToggle />
          <CartButton />
        </div>
      </div>
    </nav>
  );
}

export default Navbar;
