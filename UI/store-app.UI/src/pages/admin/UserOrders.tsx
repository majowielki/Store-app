import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useGetOrdersByUserQuery } from '@/api/orders';
import PageNumbers from '@/components/PageNumbers';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { formatAsDollars } from '@/utils';

const PAGE_SIZE = 20;

const UserOrders = () => {
  const { id = '' } = useParams<{ id: string }>();
  const [page, setPage] = useState(1);
  const { data, isLoading } = useGetOrdersByUserQuery({ userId: id, page, pageSize: PAGE_SIZE });
  const customer = data?.items[0]?.customerName;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Orders for {customer ?? `user ${id}`}</h2>
        <Button asChild variant="outline" size="sm">
          <Link to="/admin/users">Back to Users</Link>
        </Button>
      </div>
      <Card className="p-2">
        {isLoading ? (
          <div className="p-6">Loading...</div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead>Customer</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Total Items</TableHead>
                <TableHead>Total</TableHead>
                <TableHead>Date</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data?.items.map((o) => (
                <TableRow key={o.id}>
                  <TableCell>{o.id}</TableCell>
                  <TableCell>{o.customerName}</TableCell>
                  <TableCell>{o.userEmail}</TableCell>
                  <TableCell>{o.totalItems}</TableCell>
                  <TableCell>{formatAsDollars(o.total)}</TableCell>
                  <TableCell>{new Date(o.createdAt).toLocaleString()}</TableCell>
                  <TableCell className="text-right">
                    <Button asChild variant="outline" size="sm">
                      <Link to={`/admin/orders/${o.id}`}>View</Link>
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

export default UserOrders;
