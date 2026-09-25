import { useNavigate } from 'react-router-dom';
import { MoreHorizontal } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { formatAsDollars, formatDate, type OrdersResponse } from '@/utils';


const OrdersList = ({ orders }: { orders: OrdersResponse }) => {
  const navigate = useNavigate();

  return (
    <div className="mt-10">
      <h4 className="eyebrow mb-4">total orders : {orders.totalCount}</h4>
      <div className="overflow-hidden rounded-2xl border bg-card">
        <Table>
          <TableCaption className="mb-4">A list of your recent orders.</TableCaption>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead>Name</TableHead>
              <TableHead>Address</TableHead>
              <TableHead className="w-[100px]">Products</TableHead>
              <TableHead className="w-[120px]">Cost</TableHead>
              <TableHead>Date</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {orders.items.map((order) => (
              <TableRow key={order.id} className="cursor-pointer" onClick={() => navigate(`/orders/${order.id}`)}>
                <TableCell className="font-medium">{order.customerName}</TableCell>
                <TableCell className="text-muted-foreground">{order.deliveryAddress}</TableCell>
                <TableCell className="text-center tabular-nums">{order.totalItems}</TableCell>
                <TableCell className="tabular-nums">{formatAsDollars(order.total)}</TableCell>
                <TableCell className="text-muted-foreground">{formatDate(order.createdAt)}</TableCell>
                <TableCell className="text-right" onClick={(e) => e.stopPropagation()}>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="icon" className="h-8 w-8">
                        <span className="sr-only">Open menu</span>
                        <MoreHorizontal />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuLabel>Actions</DropdownMenuLabel>
                      <DropdownMenuItem onClick={() => navigator.clipboard.writeText(String(order.id))}>Copy order ID</DropdownMenuItem>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem onClick={() => navigate(`/orders/${order.id}`)}>Order details</DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
};
export default OrdersList;
