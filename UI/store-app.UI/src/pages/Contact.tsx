import { useState } from 'react';
import { Clock, Mail, MapPin, Phone } from 'lucide-react';
import { fieldLabelClass } from '@/components/FormInput';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { toast } from '@/hooks/use-toast';
import { usePageMeta } from '@/seo';

const details = [
  { icon: MapPin, label: 'Visit', value: 'Strzegomska 140A, 54-429 Wrocław' },
  { icon: Phone, label: 'Call', value: '+48 000 000 000' },
  { icon: Mail, label: 'Write', value: 'contact@store.com', href: 'mailto:contact@store.com' },
  { icon: Clock, label: 'Hours', value: 'Mon–Fri, 9:00–17:00' },
];

const Contact = () => {
  usePageMeta({ title: 'Contact', description: 'Questions about a piece, an order or a delivery: write to us, we answer within a working day.' });
  const [form, setForm] = useState({ name: '', email: '', message: '' });
  const [errors, setErrors] = useState<{ name?: string; email?: string; message?: string }>({});

  const validate = () => {
    const newErrors: typeof errors = {};
    if (!form.name.trim()) newErrors.name = 'Full name is required';
    if (!form.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(form.email)) {
      newErrors.email = 'Invalid email address';
    }
    if (!form.message.trim()) newErrors.message = 'Message is required';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    toast({ description: 'Message sent! We will contact you soon.' });
    setForm({ name: '', email: '', message: '' });
    setErrors({});
  };

  const error = (message?: string) => (message ? <p className="animate-fade-up text-xs text-destructive">{message}</p> : null);

  return (
    <div>
      <header className="max-w-3xl animate-fade-up">
        <p className="eyebrow">Contact</p>
        <h1 className="display mt-4 text-5xl leading-[0.95] md:text-7xl">
          Let&apos;s <em className="text-brand">talk.</em>
        </h1>
        <p className="mt-6 max-w-lg text-lg text-muted-foreground">
          Have a question about a piece, a delivery or an order? Send us a message and we will answer within a working day.
        </p>
      </header>

      <div className="mt-14 grid items-start gap-8 lg:grid-cols-12">
        <form className="grid gap-5 rounded-3xl border bg-card p-6 md:p-10 lg:col-span-7" onSubmit={handleSubmit} noValidate>
          <div className="grid gap-5 sm:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="contact-name" className={fieldLabelClass}>
                Full name
              </Label>
              <Input id="contact-name" name="name" value={form.name} onChange={handleChange} autoComplete="name" />
              {error(errors.name)}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="contact-email" className={fieldLabelClass}>
                Email
              </Label>
              <Input id="contact-email" name="email" type="email" value={form.email} onChange={handleChange} autoComplete="email" />
              {error(errors.email)}
            </div>
          </div>
          <div className="grid gap-2">
            <Label htmlFor="contact-message" className={fieldLabelClass}>
              Message
            </Label>
            <Textarea id="contact-message" name="message" rows={6} value={form.message} onChange={handleChange} placeholder="How can we help?" />
            {error(errors.message)}
          </div>
          <Button type="submit" size="lg" className="justify-self-start">
            Send message
          </Button>
        </form>

        <aside className="grid gap-6 lg:col-span-5">
          <ul className="grid gap-px overflow-hidden rounded-3xl border bg-border sm:grid-cols-2">
            {details.map(({ icon: Icon, label, value, href }) => (
              <li key={label} className="bg-card p-6">
                <Icon className="h-5 w-5 text-brand" />
                <p className="eyebrow mt-4">{label}</p>
                {href ? (
                  <a href={href} className="link-underline mt-1 inline-block text-sm">
                    {value}
                  </a>
                ) : (
                  <p className="mt-1 text-sm">{value}</p>
                )}
              </li>
            ))}
          </ul>
          <div className="overflow-hidden rounded-3xl border">
            <iframe
              title="Mapa"
              src="https://maps.google.com/maps?q=Strzegomska%20140A%2054-429%20Wrocław&t=&z=15&ie=UTF8&iwloc=&output=embed"
              className="h-72 w-full grayscale transition-[filter] duration-700 hover:grayscale-0"
            />
          </div>
        </aside>
      </div>
    </div>
  );
};

export default Contact;
