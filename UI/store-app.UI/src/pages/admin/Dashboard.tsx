import { CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { useGetFunnelQuery } from '@/api/funnel';
import { useGetOrderStatsQuery } from '@/api/orders';
import FunnelChart from '@/components/FunnelChart';
import { dashboardDays } from '@/config';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { formatAsDollars } from '@/utils';

const DAYS = dashboardDays;

const Dashboard = () => {
  // The same window as the order statistics, so its last stage is the order count above
  const { data: funnel, isError: funnelFailed } = useGetFunnelQuery({ days: DAYS });
  const days = funnel?.days ?? DAYS;
  const { data: stats, isLoading, isError } = useGetOrderStatsQuery({ days });

  if (isLoading) return <div>Loading dashboard...</div>;
  if (isError || !stats) return <div className="text-red-500">Failed to load dashboard stats.</div>;

  const daily = stats.daily.map((bucket) => ({
    date: bucket.bucketStart.slice(0, 10),
    revenue: bucket.revenue,
    orders: bucket.orders,
  }));
  const topProducts = [...stats.topProducts].sort((a, b) => b.quantity - a.quantity);

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>Placed order value</CardTitle>
            <p className="text-xs text-muted-foreground">Includes unpaid and cancelled orders; not collected revenue.</p>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{formatAsDollars(stats.totalRevenue)}</p>
            <p className="text-xs text-muted-foreground">Last {days} days</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Orders</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{stats.totalOrders}</p>
            <p className="text-xs text-muted-foreground">Last {days} days</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Purchase funnel</CardTitle>
          <p className="text-xs text-muted-foreground">Event counts, not unique visitors. Browser events may be missing, so later stages can exceed earlier ones.</p>
          <p className="text-xs text-muted-foreground">Last {days} days: products viewed and put in the bag in the shop, and the orders placed</p>
        </CardHeader>
        <CardContent>
          {funnel ? (
            <FunnelChart stages={funnel.stages} />
          ) : (
            <p className="text-sm text-muted-foreground">{funnelFailed ? 'The funnel could not be loaded.' : 'Loading the funnel...'}</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Placed value & Orders (Daily)</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="w-full h-72">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={daily} margin={{ top: 16, right: 24, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                <YAxis yAxisId="left" tick={{ fontSize: 12 }} />
                <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 12 }} />
                <Tooltip />
                <Legend />
                <Line yAxisId="left" type="monotone" dataKey="revenue" stroke="var(--chart-1)" name="Placed value" dot={false} />
                <Line yAxisId="right" type="monotone" dataKey="orders" stroke="var(--chart-2)" name="Orders" dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Top Products</CardTitle>
          <p className="text-xs text-muted-foreground">Ordered quantities and line values before discounts, including unpaid and cancelled orders.</p>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Product</TableHead>
                  <TableHead>Quantity</TableHead>
                  <TableHead>Gross line value</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {topProducts.length > 0 ? (
                  topProducts.map((product) => (
                    <TableRow key={product.productId}>
                      <TableCell>{product.productTitle}</TableCell>
                      <TableCell>{product.quantity}</TableCell>
                      <TableCell>{formatAsDollars(product.revenue)}</TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={3} className="text-center text-muted-foreground py-4">
                      No data
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default Dashboard;
