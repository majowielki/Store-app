import { useEffect, useMemo, useState } from 'react';
import { Card } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import type { UserResponse } from '@/utils/types';

type BackendUsersResponse = {
  items: UserResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages?: number;
};
import { identityAdminApi } from '@/utils/api';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Pagination, PaginationContent, PaginationItem, PaginationLink } from '@/components/ui/pagination';
import { Link } from 'react-router-dom';

const Users = () => {
  const [items, setItems] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState('');
  const [isActive, setIsActive] = useState<string>('');

  const filters = useMemo(() => ({ search: search.trim() || undefined, isActive: isActive === '' ? undefined : isActive === 'true' }), [search, isActive]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      try {
  const res: BackendUsersResponse = await identityAdminApi.getUsers({ ...filters, page, pageSize });
  setItems(res.items);
  // Calculate totalPages if not present
  setTotalPages(res.totalPages ?? Math.ceil((res.totalCount ?? 0) / pageSize));
      } catch {
        setItems([]);
      } finally {
        setLoading(false);
      }
    })();
  }, [filters, page, pageSize]);

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold">Users</h2>
      <Card className="p-2 space-y-3">
        <div className="flex flex-wrap items-end gap-2 p-2">
          <div>
            <Label htmlFor="search">Search</Label>
            <Input id="search" value={search} onChange={(e) => { setPage(1); setSearch(e.target.value); }} placeholder="email or name" className="w-56" />
          </div>
          <div>
            <Label htmlFor="isActive">Active</Label>
            <select id="isActive" className="border rounded h-9 px-2" value={isActive} onChange={(e) => { setPage(1); setIsActive(e.target.value); }}>
              <option value="">All</option>
              <option value="true">Active</option>
              <option value="false">Inactive</option>
            </select>
          </div>
        </div>
        {loading ? (
          <div className="p-6">Loading...</div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Roles</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((u) => (
                <TableRow key={u.id}>
                  <TableCell>{u.id}</TableCell>
                  <TableCell>{u.email}</TableCell>
                  <TableCell>{u.displayName || `${u.firstName ?? ''} ${u.lastName ?? ''}`}</TableCell>
                  <TableCell>{Array.isArray(u.roles) ? u.roles.join(', ') : ''}</TableCell>
                  <TableCell>
                    {u.isActive ? <span className="text-green-600">Active</span> : <span className="text-muted-foreground">Inactive</span>}
                  </TableCell>
                  <TableCell className="text-right space-x-2">
                    <Button asChild size="sm" variant="outline">
                      <Link to={`/admin/users/${u.id}`}>Details</Link>
                    </Button>
                    <Button asChild size="sm" variant="outline">
                      <Link to={`/admin/users/${u.id}/orders`}>Orders</Link>
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
        <div className="px-2 py-3">
          <Pagination>
            <PaginationContent>
              {Array.from({ length: totalPages }, (_, i) => (
                <PaginationItem key={i}>
                  <PaginationLink to="#" isActive={page === i + 1} onClick={(e) => { e.preventDefault(); setPage(i + 1); }}>{i + 1}</PaginationLink>
                </PaginationItem>
              ))}
            </PaginationContent>
          </Pagination>
        </div>
      </Card>
    </div>
  );
};

export default Users;
