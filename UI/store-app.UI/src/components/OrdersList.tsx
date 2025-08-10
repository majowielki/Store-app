import { useLoaderData } from 'react-router-dom';
import { type OrdersResponse } from '@/utils';
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

const OrdersList = () => {
  const ordersResponse = useLoaderData() as OrdersResponse;
  const orders = ordersResponse.orders;

  return (
    <div className='mt-16'>
      <h4 className='mb-4 capitalize'>
  total orders : {ordersResponse.totalCount}
      </h4>
      <Table>
        <TableCaption>A list of your recent orders.</TableCaption>
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Address</TableHead>
            <TableHead className='w-[100px]'>Products</TableHead>
            <TableHead className='w-[100px]'>Cost</TableHead>
            <TableHead>Date</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {orders.map((order) => {
            return (
              <TableRow key={order.id}>
                <TableCell>{order.customerName}</TableCell>
                <TableCell>{order.deliveryAddress}</TableCell>
                <TableCell className='text-center'>{order.totalItems}</TableCell>
                <TableCell>{order.orderTotal}</TableCell>
                <TableCell>{new Date(order.createdAt).toDateString()}</TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}
export default OrdersList;