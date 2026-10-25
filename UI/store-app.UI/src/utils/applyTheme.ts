import { Theme } from "@/features/theme/themeSlice";

/** The media query of a system set to dark: the "system" theme follows it. */
export const DARK_SCHEME_QUERY = "(prefers-color-scheme: dark)";

export function applyTheme(theme: Theme) {
  const root = window.document.documentElement;
  root.classList.remove("light", "dark");

  if (theme === "system") {
    const systemTheme = window.matchMedia(DARK_SCHEME_QUERY).matches
      ? "dark"
      : "light";
    root.classList.add(systemTheme);
    return;
  }
  root.classList.add(theme);
}
