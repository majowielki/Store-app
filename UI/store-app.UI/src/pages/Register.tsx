import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardContent, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { SubmitBtn, FormInput } from '@/components';
import { signIn } from '@/features/session/sessionThunks';
import { useAppDispatch } from '@/hooks';
import { toast } from '@/hooks/use-toast';
import { validateRegister } from '@/utils/validation';
import { closeAuthPage } from '@/utils/closeAuthPage';

const Register = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const validation = validateRegister(form);
    setErrors(validation);
    if (Object.keys(validation).length > 0) return;
    setSubmitting(true);
    try {
      await dispatch(signIn({ kind: 'register', details: form })).unwrap();
      toast({ description: 'Successfully registered!' });
      navigate('/');
    } catch {
      // Reported by the error middleware (a taken e-mail, a weak password)
      setSubmitting(false);
    }
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
          <CardTitle className="text-center">Register</CardTitle>
        </CardHeader>
        <CardContent>
          <form method="post" onSubmit={handleSubmit}>
            <FormInput type="text" name="firstName" value={form.firstName} onChange={handleChange} autoComplete="given-name" />
            {errors.firstName && <div className="text-red-500 text-xs mb-1">{errors.firstName}</div>}
            <FormInput type="text" name="lastName" value={form.lastName} onChange={handleChange} autoComplete="family-name" />
            {errors.lastName && <div className="text-red-500 text-xs mb-1">{errors.lastName}</div>}
            <FormInput type="email" name="email" value={form.email} onChange={handleChange} autoComplete="email" />
            {errors.email && <div className="text-red-500 text-xs mb-1">{errors.email}</div>}
            <FormInput type="password" name="password" value={form.password} onChange={handleChange} autoComplete="new-password" />
            {errors.password && <div className="text-red-500 text-xs mb-1">{errors.password}</div>}
            <FormInput type="password" name="confirmPassword" value={form.confirmPassword} onChange={handleChange} autoComplete="new-password" />
            {errors.confirmPassword && <div className="text-red-500 text-xs mb-1">{errors.confirmPassword}</div>}
            <SubmitBtn text="Register" className="w-full mt-4" isSubmitting={submitting} />
            <p className="text-center mt-4">
              Already a member?{' '}
              <Button type="button" asChild variant="link">
                <Link to="/login">Login</Link>
              </Button>
            </p>
          </form>
        </CardContent>
      </Card>
    </section>
  );
};
export default Register;
