import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { SubmitBtn, FormInput } from '@/components';
import AuthShell from '@/components/AuthShell';
import picture from '@/assets/sign-in.webp';
import { isAdmin } from '@/features/session/roles';
import { signIn, type SignInRequest } from '@/features/session/sessionThunks';
import { useAppDispatch } from '@/hooks';
import { toast } from '@/hooks/use-toast';
import { validateLogin } from '@/utils/validation';

const FieldError = ({ message }: { message?: string }) =>
  message ? <p className="-mt-2 animate-fade-up text-xs text-destructive">{message}</p> : null;

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
    <AuthShell eyebrow="Sign in" title={<>Welcome <em>back.</em></>} lead="Sign in to see your orders and check out faster." image={picture}>
      <form method="post" className="grid gap-5" onSubmit={handleSubmit} noValidate>
        <FormInput type="email" name="email" value={form.email} onChange={handleChange} autoComplete="email" />
        <FieldError message={errors.email} />
        <FormInput type="password" name="password" value={form.password} onChange={handleChange} autoComplete="current-password" />
        <FieldError message={errors.password} />
        <SubmitBtn text="Login" className="mt-2 w-full" isSubmitting={busy === 'login'} disabled={busy !== null} />
      </form>

      <div className="my-8 flex items-center gap-4 text-xs uppercase tracking-[0.14em] text-muted-foreground">
        <span className="h-px flex-1 bg-border" />
        or look around first
        <span className="h-px flex-1 bg-border" />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <Button type="button" variant="outline" disabled={busy !== null} onClick={() => start({ kind: 'demoUser' }, 'Demo user logged in!')}>
          Demo User
        </Button>
        <Button type="button" variant="outline" disabled={busy !== null} onClick={() => start({ kind: 'demoAdmin' }, 'Demo admin logged in!')}>
          Demo Admin
        </Button>
      </div>

      <p className="mt-10 text-center text-sm text-muted-foreground">
        Not a member?{' '}
        <Link to="/register" className="link-underline font-medium text-foreground">
          Register
        </Link>
      </p>
    </AuthShell>
  );
};
export default Login;
