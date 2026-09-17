import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ChevronDown, ChevronUp, Trash2 } from 'lucide-react';
import { useDeleteProductMutation, useGetProductsAdminQuery } from '@/api/catalog';
import type { Product } from '@/api/types';
import ConfirmDialog from '@/components/ConfirmDialog';
import PageNumbers from '@/components/PageNumbers';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { toast } from '@/hooks/use-toast';
import { formatAsDollars } from '@/utils';

// The values the admin listing sorts by (the API documents them as free text)
type SortKey = 'id' | 'title' | 'price' | 'company';
type SortDir = 'asc' | 'desc';

const PAGE_SIZE = 10;

const Products = () => {
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState<{ key: SortKey; direction: SortDir }>({ key: 'id', direction: 'asc' });
  const { data, isLoading } = useGetProductsAdminQuery({ page, pageSize: PAGE_SIZE, sortBy: sort.key, sortDir: sort.direction });
  const [deleteProduct, { isLoading: deleting }] = useDeleteProductMutation();
  const [toDelete, setToDelete] = useState<Product | null>(null);

  const handleSort = (key: SortKey) => {
    setSort((prev) => (prev.key === key ? { key, direction: prev.direction === 'asc' ? 'desc' : 'asc' } : { key, direction: 'asc' }));
  };

  // A refusal (the demo administrator, 403) has been reported by the error middleware
  const confirmDelete = async () => {
    if (!toDelete) return;
    try {
      await deleteProduct(toDelete.id).unwrap();
      toast({ description: 'Product deleted.' });
    } catch {
      // reported
    } finally {
      setToDelete(null);
    }
  };

  const SortButton = ({ sortKey, label }: { sortKey: SortKey; label: string }) => {
    const isActive = sort.key === sortKey;
    return (
      <button
        type="button"
        className={`flex items-center gap-1 px-1 py-0.5 rounded transition-colors ${isActive ? 'bg-muted text-primary' : 'hover:bg-accent text-muted-foreground'}`}
        onClick={() => handleSort(sortKey)}
        aria-label={`Sort by ${label}`}
      >
        <span>{label}</span>
        <span className="flex flex-col">
          <ChevronUp className={`w-3 h-3 -mb-1 ${isActive && sort.direction === 'asc' ? 'text-primary' : 'text-muted-foreground'}`} />
          <ChevronDown className={`w-3 h-3 -mt-1 ${isActive && sort.direction === 'desc' ? 'text-primary' : 'text-muted-foreground'}`} />
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
        {isLoading ? (
          <div className="p-6">Loading...</div>
        ) : (
          <>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="select-none">
                    <SortButton sortKey="id" label="ID" />
                  </TableHead>
                  <TableHead className="select-none">
                    <SortButton sortKey="title" label="Title" />
                  </TableHead>
                  <TableHead className="select-none">
                    <SortButton sortKey="price" label="Price" />
                  </TableHead>
                  <TableHead className="select-none">
                    <SortButton sortKey="company" label="Company" />
                  </TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.items.map((p) => (
                  <TableRow key={p.id}>
                    <TableCell>{p.id}</TableCell>
                    <TableCell>{p.title}</TableCell>
                    <TableCell>{formatAsDollars(p.price)}</TableCell>
                    <TableCell>{p.company}</TableCell>
                    <TableCell>{p.isActive ? 'Active' : <span className="text-muted-foreground">Inactive</span>}</TableCell>
                    <TableCell className="text-right space-x-2 flex items-center justify-end gap-2">
                      <Button asChild size="sm" variant="outline">
                        <Link to={`/admin/products/${p.id}`}>Edit</Link>
                      </Button>
                      <Button size="sm" variant="destructive" aria-label={`Delete ${p.title}`} onClick={() => setToDelete(p)} disabled={!p.isActive}>
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <PageNumbers page={page} totalPages={data?.totalPages ?? 1} onPageChange={setPage} />
          </>
        )}
      </Card>
      <ConfirmDialog
        open={toDelete !== null}
        title="Delete product"
        description={toDelete ? `"${toDelete.title}" will be removed from the shop. It can be restored from the edit form.` : ''}
        confirmLabel="Delete"
        busy={deleting}
        onConfirm={confirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </div>
  );
};

export default Products;
