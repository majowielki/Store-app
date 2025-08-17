import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { customFetch } from '@/utils';
import type { ProductsResponse, ProductData } from '@/utils/types';

import { Card } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Trash2, ChevronUp, ChevronDown } from 'lucide-react';
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationPrevious,
  PaginationNext,
} from '@/components/ui/pagination';

const Products = () => {

  const [items, setItems] = useState<ProductData[]>([]);
  const [meta, setMeta] = useState<ProductsResponse['meta'] | null>(null);
  const [loading, setLoading] = useState(true);
  const [sort, setSort] = useState<{ key: keyof ProductData | 'company' | 'price' | 'title'; direction: 'asc' | 'desc' }>({ key: 'id', direction: 'asc' });
  // Pagination state
  const [page, setPage] = useState(1);
  const pageSize = 10;

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        // Użyj endpointu admina i przekazuj sortBy oraz sortDir
        const params: Record<string, string | number> = {
          page,
          pageSize,
          sortBy: sort.key,
          sortDir: sort.direction,
        };
  const res = await customFetch.get<ProductsResponse>('/products/admin', { params });
  console.log('API /api/products/admin response:', res.data);
  setItems(res.data.data);
  setMeta(res.data.meta);
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, [page, pageSize, sort]);

  const handleSort = (key: keyof ProductData | 'company' | 'price' | 'title') => {
    setSort((prev) => {
      if (prev.key === key) {
        return { key, direction: prev.direction === 'asc' ? 'desc' : 'asc' };
      }
      return { key, direction: 'asc' };
    });
  };


  // Nie sortujemy lokalnie, sortowanie po stronie backendu
  const sortedItems = items;

  // Pagination logic (backend-driven)
  // const totalProducts = meta?.pagination?.total ?? 0;
  const totalPages = meta?.pagination?.pageCount ?? 1;
  const paginatedItems = sortedItems;


  // Sort button with icon
  const SortButton = (key: keyof ProductData | 'company' | 'price' | 'title', label: string) => {
    const isActive = sort.key === key;
    return (
      <button
        type="button"
        className={`flex items-center gap-1 px-1 py-0.5 rounded transition-colors ${isActive ? 'bg-muted text-primary' : 'hover:bg-accent text-muted-foreground'}`}
        onClick={() => handleSort(key)}
        aria-label={`Sort by ${label}`}
      >
        <span>{label}</span>
        <span className="flex flex-col">
          <ChevronUp
            className={`w-3 h-3 -mb-1 ${isActive && sort.direction === 'asc' ? 'text-primary' : 'text-muted-foreground'}`}
          />
          <ChevronDown
            className={`w-3 h-3 -mt-1 ${isActive && sort.direction === 'desc' ? 'text-primary' : 'text-muted-foreground'}`}
          />
        </span>
      </button>
    );
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Products</h2>
        <Button asChild>
          <Link to="/admin/products/new">Add product</Link>
        </Button>
      </div>
      <Card className="p-2">
        {loading ? (
          <div className="p-6">Loading...</div>
        ) : (
          <>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="select-none">
                    {SortButton('id', 'ID')}
                  </TableHead>
                  <TableHead className="select-none">
                    {SortButton('title', 'Title')}
                  </TableHead>
                  <TableHead className="select-none">
                    {SortButton('price', 'Price')}
                  </TableHead>
                  <TableHead className="select-none">
                    {SortButton('company', 'Company')}
                  </TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paginatedItems.map((p) => (
                  <TableRow key={p.id}>
                    <TableCell>{p.id}</TableCell>
                    <TableCell>{p.attributes.title}</TableCell>
                    <TableCell>{p.attributes.price}</TableCell>
                    <TableCell>{p.attributes.company}</TableCell>
                    <TableCell className="text-right space-x-2 flex items-center justify-end gap-2">
                      <Button asChild size="sm" variant="outline"><Link to={`/admin/products/${p.id}`}>Edit</Link></Button>
                      <Button size="sm" variant="destructive" aria-label="Delete"><Trash2 className="w-4 h-4" /></Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            {/* Pagination controls */}
            {totalPages > 1 && (
              <Pagination className="mt-4">
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious
                      to="#"
                      onClick={e => { e.preventDefault(); setPage(p => Math.max(1, p - 1)); }}
                      className={page === 1 ? 'pointer-events-none opacity-50' : ''}
                    />
                  </PaginationItem>
                  {Array.from({ length: totalPages }).map((_, i) => (
                    <PaginationItem key={i}>
                      <PaginationLink
                        to="#"
                        isActive={page === i + 1}
                        onClick={e => { e.preventDefault(); setPage(i + 1); }}
                      >
                        {i + 1}
                      </PaginationLink>
                    </PaginationItem>
                  ))}
                  <PaginationItem>
                    <PaginationNext
                      to="#"
                      onClick={e => { e.preventDefault(); setPage(p => Math.min(totalPages, p + 1)); }}
                      className={page === totalPages ? 'pointer-events-none opacity-50' : ''}
                    />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            )}
          </>
        )}
      </Card>
    </div>
  );
};

export default Products;
