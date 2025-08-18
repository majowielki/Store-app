import React, { useEffect, useState } from "react";
import hero1 from "@/assets/hero1.webp";
import { useNavigate } from "react-router-dom";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";

const AUTH_TOKEN_KEY = "token"; // Change if your auth key is different
const IMAGE_BOX_HIDE_UNTIL_KEY = "imageBoxHideUntil";
const HIDE_DURATION_MS = 30 * 1000; // 30 seconds

export const LandingImageBox: React.FC = () => {
  const [visible, setVisible] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    const isLoggedIn = !!localStorage.getItem(AUTH_TOKEN_KEY);
    const hideUntil = localStorage.getItem(IMAGE_BOX_HIDE_UNTIL_KEY);
    const now = Date.now();
    if (!isLoggedIn && (!hideUntil || now > Number(hideUntil))) {
      setVisible(true);
    } else {
      setVisible(false);
    }
  }, []);

  const handleClose = (e: React.MouseEvent) => {
    e.stopPropagation();
    localStorage.setItem(IMAGE_BOX_HIDE_UNTIL_KEY, String(Date.now() + HIDE_DURATION_MS));
    setVisible(false);
  };

  const handleBoxClick = () => {
    navigate("/register");
  };

  if (!visible) return null;

  return (
    <div
      className="relative mx-auto my-8 max-w-xl cursor-pointer rounded-lg border bg-white shadow-lg"
      onClick={handleBoxClick}
      tabIndex={0}
      aria-label="Register promotion"
    >
      <Button
        size="icon"
        variant="ghost"
        className="absolute right-2 top-2 z-10"
        onClick={handleClose}
        tabIndex={-1}
        aria-label="Close"
      >
        <X className="h-5 w-5" />
      </Button>
      <img
        src={hero1}
        alt="Register and get exclusive offers!"
        className="w-full rounded-lg"
        draggable={false}
      />
      {/* <div className="absolute bottom-4 left-0 w-full text-center text-lg font-semibold text-white drop-shadow-lg">
        Register now and get exclusive offers!
      </div> */}
    </div>
  );
};
