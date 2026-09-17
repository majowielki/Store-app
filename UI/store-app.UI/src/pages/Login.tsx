import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardContent, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { SubmitBtn, FormInput } from '@/components';
import { isAdmin } from '@/features/session/roles';
import { signIn, type SignInRequest } from '@/features/session/sessionThunks';
import { useAppDispatch } from '@/hooks';
import { toast } from '@/hooks/use-toast';
import { validateLogin } from '@/utils/validation';
import { closeAuthPage } from '@/utils/closeAuthPage';

const Login = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [form, setForm] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState<SignInRequest['kind'] | null>(null);

  // A failed sign-in has been reported with a toast by the error middleware
  const start = async (request: SignInRequest, welcome: string) => {
    setBusy(request.kind);
    try {
      const user = await dispatch(signIn(request)).unwrap();
      toast({ description: welcome });
      navigate(isAdmin(user) ? '/admin' : '/');
    } catch {
      setBusy(null);
    }
  };

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const validation = validateLogin(form);
    setErrors(validation);
    if (Object.keys(validation).length > 0) return;
    void start({ kind: 'login', credentials: form }, 'Successfully logged in!');
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  return (
    <section className="h-screen grid place-items-center">
      <Card className="w-96 bg-muted relative">
        <button
          type="button"
          onClick={closeAuthPage}
          className="absolute top-2 right-2 text-xl px-2 py-1 rounded hover:bg-gray-200"
          title="Close"
          aria-label="Close"
        >
          ×
        </button>
        <CardHeader>
          <CardTitle className="text-center">Login</CardTitle>
        </CardHeader>
        <CardContent>
          <form method="post" className="space-y-4" onSubmit={handleSubmit}>
            <FormInput type="email" name="email" value={form.email} onChange={handleChange} autoComplete="email" />
            {errors.email && <div className="text-red-500 text-xs mb-1">{errors.email}</div>}
            <FormInput type="password" name="password" value={form.password} onChange={handleChange} autoComplete="current-password" />
            {errors.password && <div className="text-red-500 text-xs mb-1">{errors.password}</div>}
            <SubmitBtn text="Login" className="w-full mt-4" isSubmitting={busy === 'login'} disabled={busy !== null} />
            <div className="flex gap-2 mt-4">
              <Button
                type="button"
                variant="outline"
                className="w-1/2"
                disabled={busy !== null}
                onClick={() => start({ kind: 'demoUser' }, 'Demo user logged in!')}
              >
                Demo User
              </Button>
              <Button
                type="button"
                variant="outline"
                className="w-1/2"
                disabled={busy !== null}
                onClick={() => start({ kind: 'demoAdmin' }, 'Demo admin logged in!')}
              >
                Demo Admin
              </Button>
            </div>
            <p className="text-center mt-4">
              Not a member?{' '}
              <Button type="button" asChild variant="link">
                <Link to="/register">Register</Link>
              </Button>
            </p>
          </form>
        </CardContent>
      </Card>
    </section>
  );
};
export default Login;
