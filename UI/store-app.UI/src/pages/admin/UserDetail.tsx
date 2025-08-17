import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { identityAdminApi } from '@/utils/api';
import type { UserResponse } from '@/utils/types';
import { Button } from '@/components/ui/button';

const UserDetail = () => {
  const { id } = useParams<{ id: string }>();
  const [user, setUser] = useState<UserResponse | null>(null);

  useEffect(() => {
    if (!id) return;
    (async () => {
      try {
        const u = await identityAdminApi.getUser(id);
        setUser(u);
      } catch {
        setUser(null);
      }
    })();
  }, [id]);

  if (!user) return <div>Loading...</div>;

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
