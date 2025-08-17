import { useNavigate, useSearchParams } from 'react-router-dom';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { useEffect, useRef, useState } from 'react';

const SearchBar = () => {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [q, setQ] = useState<string>('');
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const current = params.get('search') ?? '';
    setQ(current);
  }, [params]);

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const query = q.trim();
    const url = query ? `/products?search=${encodeURIComponent(query)}` : '/products';
    navigate(url);
  };

  return (
    <form onSubmit={onSubmit} className="flex w-full max-w-xl items-center gap-2">
      <Input
        ref={inputRef}
        value={q}
        onChange={(e) => setQ(e.target.value)}
        placeholder="Search products..."
        aria-label="Search products"
      />
      <Button type="submit" variant="default">Search</Button>
    </form>
  );
};

export default SearchBar;
