import { Link } from "react-router-dom";
import { House } from "lucide-react";

function Logo() {
  return (
    <Link
      to="/"
      className="hidden lg:flex justify-center items-center bg-primary p-2 rounded-lg text-white"
    >
      <House className="w-8 h-8" />
    </Link>
  );
}
export default Logo;
