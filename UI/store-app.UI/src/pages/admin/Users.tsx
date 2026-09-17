import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useGetAdminUsersQuery } from '@/api/admin';
import PageNumbers from '@/components/PageNumbers';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';

const PAGE_SIZE = 20;

const Users = () => {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [isActive, setIsActive] = useState('');
  const { data, isLoading } = useGetAdminUsersQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search.trim() || undefined,
    isActive: isActive === '' ? undefined : isActive === 'true',
  });

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold">Users</h2>
      <Card className="p-2 space-y-3">
        <div className="flex flex-wrap items-end gap-2 p-2">
          <div>
            <Label htmlFor="search">Search</Label>
            <Input
              id="search"
              value={search}
              onChange={(e) => {
                setPage(1);
                setSearch(e.target.value);
              }}
              placeholder="email or name"
              className="w-56"
            />
          </div>
          <div>
            <Label htmlFor="isActive">Active</Label>
            <select
              id="isActive"
              className="border rounded h-9 px-2"
              value={isActive}
              onChange={(e) => {
                setPage(1);
                setIsActive(e.target.value);
              }}
            >
              <option value="">All</option>
              <option value="true">Active</option>
              <option value="false">Inactive</option>
            </select>
          </div>
        </div>
        {isLoading ? (
          <div className="p-6">Loading...</div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Email</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Last login</TableHead>
                <TableHead className="text-right">Orders</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data?.items.map((u) => (
                <TableRow key={u.id}>
                  <TableCell>
                    <Link to={`/admin/users/${u.id}`} className="hover:underline">
                      {u.email}
                    </Link>
                  </TableCell>
                  <TableCell>{`${u.firstName} ${u.lastName}`.trim()}</TableCell>
                  <TableCell>
                    {u.isActive ? <span className="text-green-600">Active</span> : <span className="text-muted-foreground">Inactive</span>}
                  </TableCell>
                  <TableCell>{u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString() : 'never'}</TableCell>
                  <TableCell className="text-right space-x-2">
                    <Button asChild size="sm" variant="outline">
                      <Link to={`/admin/users/${u.id}/orders`}>Orders</Link>
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
        <PageNumbers page={page} totalPages={data?.totalPages ?? 1} onPageChange={setPage} />
      </Card>
    </div>
  );
};

export default Users;
