import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Card } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { formatAsDollars } from '@/utils';
import type { OrdersResponse, Order } from '@/utils/types';
import { identityAdminApi } from '@/utils/api';
import { Pagination, PaginationContent, PaginationItem, PaginationLink } from '@/components/ui/pagination';

const UserOrders = () => {
  const { id } = useParams<{ id: string }>();
  const [data, setData] = useState<OrdersResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);

  useEffect(() => {
    if (!id) return;
    (async () => {
      setLoading(true);
      try {
        const res = await identityAdminApi.getUserOrders(id, page, pageSize);
        setData(res);
      } finally {
        setLoading(false);
      }
    })();
  }, [id, page, pageSize]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">
          Orders for {data?.items?.[0]?.customerName ? `${data.items[0].customerName}` : `user ${id}`}
        </h2>
        <Button asChild variant="outline" size="sm">
          <Link to="/admin/users">Back to Users</Link>
        </Button>
      </div>
      <Card className="p-2">
        {loading ? (
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
              {data?.items?.map((o: Order) => (
                <TableRow key={o.id}>
                  <TableCell>{o.id}</TableCell>
                  <TableCell>{o.customerName}</TableCell>
                  <TableCell>{o.userEmail}</TableCell>
                  <TableCell>{o.totalItems}</TableCell>
                  <TableCell>{formatAsDollars(o.orderTotal)}</TableCell>
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
        <div className="px-2 py-3">
          <Pagination>
            <PaginationContent>
              {Array.from({ length: data?.totalPages ?? 1 }, (_, i) => (
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

export default UserOrders;
