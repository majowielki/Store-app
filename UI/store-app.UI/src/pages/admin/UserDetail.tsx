import { Link, useParams } from 'react-router-dom';
import { useGetAdminUserQuery } from '@/api/admin';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

const UserDetail = () => {
  const { id = '' } = useParams<{ id: string }>();
  const { data: user, isLoading } = useGetAdminUserQuery(id);

  if (isLoading) return <div>Loading...</div>;
  if (!user) return <div>User not found.</div>;

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle>User {user.displayName || user.email}</CardTitle>
        </CardHeader>
        <CardContent className="text-sm grid md:grid-cols-2 gap-2">
          <div>
            <div>ID: {user.id}</div>
            <div>Email: {user.email}</div>
            <div>Name: {user.displayName || `${user.firstName ?? ''} ${user.lastName ?? ''}`}</div>
          </div>
          <div>
            <div>Roles: {user.roles.join(', ')}</div>
            <div>Status: {user.isActive ? 'Active' : 'Inactive'}</div>
            <div>Joined: {new Date(user.createdAt).toLocaleString()}</div>
          </div>
          <div className="md:col-span-2 mt-2">
            <Button asChild variant="outline" size="sm">
              <Link to={`/admin/users/${user.id}/orders`}>View Orders</Link>
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default UserDetail;
