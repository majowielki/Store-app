import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { SubmitBtn, FormInput } from '@/components';
import AuthShell from '@/components/AuthShell';
import picture from '@/assets/register.webp';
import { useGetPricingRulesQuery } from '@/api/orders';
import { signIn } from '@/features/session/sessionThunks';
import { useAppDispatch } from '@/hooks';
import { toast } from '@/hooks/use-toast';
import { validateRegister } from '@/utils/validation';

const FieldError = ({ message }: { message?: string }) =>
  message ? <p className="-mt-2 animate-fade-up text-xs text-destructive">{message}</p> : null;

const Register = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { data: rules } = useGetPricingRulesQuery();
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
    <AuthShell
      eyebrow="Create an account"
      title={<>Make yourself <em>at home.</em></>}
      lead={rules ? `Register and ${rules.firstOrderDiscountPercent}% comes off your first order — no code needed.` : 'Register to track your orders and check out faster.'}
      image={picture}
    >
      <form method="post" className="grid gap-5" onSubmit={handleSubmit} noValidate>
        <div className="grid gap-5 sm:grid-cols-2">
          <div className="grid gap-2">
            <FormInput type="text" name="firstName" label="first name" value={form.firstName} onChange={handleChange} autoComplete="given-name" />
            <FieldError message={errors.firstName} />
          </div>
          <div className="grid gap-2">
            <FormInput type="text" name="lastName" label="last name" value={form.lastName} onChange={handleChange} autoComplete="family-name" />
            <FieldError message={errors.lastName} />
          </div>
        </div>
        <FormInput type="email" name="email" value={form.email} onChange={handleChange} autoComplete="email" />
        <FieldError message={errors.email} />
        <FormInput type="password" name="password" value={form.password} onChange={handleChange} autoComplete="new-password" />
        <FieldError message={errors.password} />
        <FormInput
          type="password"
          name="confirmPassword"
          label="confirm password"
          value={form.confirmPassword}
          onChange={handleChange}
          autoComplete="new-password"
        />
        <FieldError message={errors.confirmPassword} />
        <SubmitBtn text="Register" className="mt-2 w-full" isSubmitting={submitting} />
      </form>
      <p className="mt-10 text-center text-sm text-muted-foreground">
        Already a member?{' '}
        <Link to="/login" className="link-underline font-medium text-foreground">
          Login
        </Link>
      </p>
    </AuthShell>
  );
};
export default Register;
