import { Monitor, Moon, Sun } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { setTheme, type Theme } from '@/features/theme/themeSlice';
import { cn } from '@/lib/utils';

const themes: { value: Theme; label: string; icon: typeof Sun }[] = [
  { value: 'light', label: 'Light', icon: Sun },
  { value: 'dark', label: 'Dark', icon: Moon },
  { value: 'system', label: 'System', icon: Monitor },
];

/** The sun/moon button of the header, with the three themes in a menu. */
const ModeToggle = ({ className }: { className?: string }) => {
  const dispatch = useAppDispatch();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className={cn('relative', className)}>
          <Sun className="h-[1.15rem] w-[1.15rem] rotate-0 scale-100 transition-all duration-500 dark:-rotate-90 dark:scale-0" />
          <Moon className="absolute h-[1.15rem] w-[1.15rem] rotate-90 scale-0 transition-all duration-500 dark:rotate-0 dark:scale-100" />
          <span className="sr-only">Toggle theme</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {themes.map(({ value, label, icon: Icon }) => (
          <DropdownMenuItem key={value} onClick={() => dispatch(setTheme(value))}>
            <Icon />
            {label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
};

/** The three themes side by side, for the mobile menu. */
export const ThemeSwitch = ({ className }: { className?: string }) => {
  const dispatch = useAppDispatch();
  const current = useAppSelector((state) => state.theme.theme);
  return (
    <div role="radiogroup" aria-label="Theme" className={cn('inline-flex rounded-full border p-1', className)}>
      {themes.map(({ value, label, icon: Icon }) => (
        <button
          key={value}
          type="button"
          role="radio"
          aria-checked={current === value}
          aria-label={label}
          onClick={() => dispatch(setTheme(value))}
          className={cn(
            'grid h-8 w-8 place-items-center rounded-full transition-colors',
            current === value ? 'bg-foreground text-background' : 'text-muted-foreground hover:text-foreground',
          )}
        >
          <Icon className="h-4 w-4" />
        </button>
      ))}
    </div>
  );
};

export default ModeToggle;
