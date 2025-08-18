import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import { orderApi } from '@/utils/api';
import type { AdminOrderStats } from '@/utils/types';
import { formatAsDollars } from '@/utils';

// Backend stats response type (matches actual API response)
type BackendStatsResponse = {
  totalRevenue: number;
  totalOrders: number;
  daily: Array<{ bucketStart: string; orders: number; revenue: number }>;
  weekly: Array<{ bucketStart: string; orders: number; revenue: number }>;
  topProducts: Array<{ productId: number; productTitle: string; quantity: number; revenue: number }>;
};
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from '@/components/ui/table';

const Dashboard = () => {
  const [stats, setStats] = useState<AdminOrderStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  // ...existing code...

  useEffect(() => {
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const s: BackendStatsResponse = await orderApi.getAdminStats(30);
        // Map backend response to AdminOrderStats shape
        const mapped = {
          days: 30,
          totals: {
            revenue: s.totalRevenue ?? 0,
            ordersCount: s.totalOrders ?? 0,
          },
          dailyBuckets: Array.isArray(s.daily)
            ? s.daily.map((d) => ({
                date: d.bucketStart || '',
                revenue: d.revenue ?? 0,
                ordersCount: d.orders ?? 0,
              }))
            : [],
          weeklyBuckets: Array.isArray(s.weekly)
            ? s.weekly.map((w) => ({
                isoWeek: '',
                startDate: w.bucketStart || '',
                endDate: '',
                revenue: w.revenue ?? 0,
                ordersCount: w.orders ?? 0,
              }))
            : [],
          topProducts: Array.isArray(s.topProducts)
            ? s.topProducts.map((p) => ({
                productId: p.productId,
                title: p.productTitle,
                quantity: p.quantity,
                revenue: p.revenue,
              }))
            : [],
        };
        setStats(mapped);
      } catch {
        setStats(null);
        setError('Failed to load dashboard stats.');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  if (loading) {
    return <div>Loading dashboard...</div>;
  }

  if (error) {
    return <div className="text-red-500">{error}</div>;
  }

  if (!stats || !stats.totals) {
    return <div>No dashboard data available.</div>;
  }

  return (
    <div className="flex flex-col gap-6">
      {/* Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>Revenue</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{formatAsDollars(stats.totals.revenue)}</p>
            <p className="text-xs text-muted-foreground">Last {stats.days ?? 30} days</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Orders</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{stats.totals.ordersCount}</p>
            <p className="text-xs text-muted-foreground">Last {stats.days ?? 30} days</p>
          </CardContent>
        </Card>
      </div>

      {/* Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Revenue & Orders (Daily)</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="w-full h-72">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={stats.dailyBuckets} margin={{ top: 16, right: 24, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                <YAxis yAxisId="left" tick={{ fontSize: 12 }} />
                <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 12 }} />
                <Tooltip />
                <Legend />
                <Line yAxisId="left" type="monotone" dataKey="revenue" stroke="#2563eb" name="Revenue" dot={false} />
                <Line yAxisId="right" type="monotone" dataKey="ordersCount" stroke="#16a34a" name="Orders" dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </CardContent>
      </Card>

      {/* Top Products Table - shadcn Table, sorted by quantity desc */}
      <Card>
        <CardHeader>
          <CardTitle>Top Products</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Product</TableHead>
                  <TableHead>Quantity</TableHead>
                  <TableHead>Revenue</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(stats.topProducts?.length ?? 0) > 0 ? (
                  [...stats.topProducts]
                    .filter(prod => prod.title !== 'First Order Discount' && prod.title !== 'Delivery Fee')
                    .sort((a, b) => b.quantity - a.quantity)
                    .map((prod) => (
                      <TableRow key={prod.productId + '-' + prod.title}>
                        <TableCell>{prod.title}</TableCell>
                        <TableCell>{prod.quantity}</TableCell>
                        <TableCell>{formatAsDollars(prod.revenue)}</TableCell>
                      </TableRow>
                    ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={3} className="text-center text-muted-foreground py-4">No data</TableCell>
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
