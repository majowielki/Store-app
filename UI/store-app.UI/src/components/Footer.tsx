import { Link } from 'react-router-dom';

const Footer = () => {
  return (
    <footer className="bg-muted/40 mt-12">
      <div className="align-element py-10 grid gap-8 md:grid-cols-4">
        <div>
          <h4 className="font-semibold mb-3">Shop</h4>
          <ul className="space-y-2 text-sm text-muted-foreground">
            <li><Link to="/">Home</Link></li>
            <li><Link to="/products">Products</Link></li>
            <li><Link to="/contact">Contact</Link></li>
            <li><Link to="/about">About us</Link></li>
          </ul>
        </div>
        <div>
          <h4 className="font-semibold mb-3">Categories</h4>
          <ul className="space-y-2 text-sm text-muted-foreground">
            <li><Link to={`/products?group=${encodeURIComponent('furniture')}`}>Furniture</Link></li>
            <li><Link to={`/products?group=${encodeURIComponent('kitchen-appliances')}`}>Kitchens & Appliances</Link></li>
            <li><Link to={`/products?group=${encodeURIComponent('bathroom')}`}>Bathroom</Link></li>
            <li><Link to={`/products?group=${encodeURIComponent('all')}`}>More</Link></li>
          </ul>
        </div>
        <div>
          <h4 className="font-semibold mb-3">Help</h4>
          <ul className="space-y-2 text-sm text-muted-foreground">
            <li>Shipping & payments</li>
            <li>Returns & complaints</li>
            <li>Terms & conditions</li>
            <li>Privacy policy</li>
          </ul>
        </div>
        <div>
          <h4 className="font-semibold mb-3">Contact details</h4>
          <p className="text-sm text-muted-foreground">Strzegomska 140A 54-429 Wrocław</p>
          <p className="text-sm text-muted-foreground">+48 000 000 000</p>
          <p className="text-sm text-muted-foreground">contact@store.com</p>
          <div className="mt-3">
            <Link to="/contact" className="text-sm text-primary hover:underline">
              Contact
            </Link>
          </div>
        </div>
      </div>
      <div className="border-t">
        <div className="align-element py-4 text-xs text-muted-foreground flex items-center justify-between">
          <span>© {new Date().getFullYear()} Store. All rights reserved.</span>
          <span>Made with ❤️</span>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
