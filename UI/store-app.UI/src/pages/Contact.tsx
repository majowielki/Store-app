import { Card, CardContent } from '@/components/ui/card';
import { useState } from 'react';
import { toast } from '@/hooks/use-toast';

const Contact = () => {
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

  return (
    <div className="grid gap-6 md:grid-cols-2">
      <Card>
        <CardContent className="p-6">
          <h2 className="text-xl font-semibold">Contact</h2>
          <p className="text-muted-foreground mt-2">Have questions? Send us a message.</p>
          <form className="mt-4 grid gap-3" onSubmit={handleSubmit} noValidate>
            <input
              className="border rounded px-3 py-2"
              placeholder="Full name"
              value={form.name}
              onChange={handleChange}
              name="name"
            />
            {errors.name && <div className="text-xs text-red-500">{errors.name}</div>}
            <input
              className="border rounded px-3 py-2"
              placeholder="Email"
              type="email"
              value={form.email}
              onChange={handleChange}
              name="email"
            />
            {errors.email && <div className="text-xs text-red-500">{errors.email}</div>}
            <textarea
              className="border rounded px-3 py-2"
              placeholder="Message"
              rows={5}
              value={form.message}
              onChange={handleChange}
              name="message"
            />
            {errors.message && <div className="text-xs text-red-500">{errors.message}</div>}
            <button className="bg-primary text-primary-foreground px-4 py-2 rounded" type="submit">Send</button>
          </form>
        </CardContent>
      </Card>
      <Card>
        <CardContent className="p-6">
          <h3 className="font-semibold">Company details</h3>
          <p className="text-sm text-muted-foreground mt-2">Strzegomska 140A 54-429 Wrocław</p>
          <p className="text-sm text-muted-foreground">+48 000 000 000</p>
          <p className="text-sm text-muted-foreground">contact@store.com</p>
          <div className="mt-4">
            <iframe
              title="Mapa"
              src="https://maps.google.com/maps?q=Strzegomska%20140A%2054-429%20Wrocław&t=&z=15&ie=UTF8&iwloc=&output=embed"
              className="w-full h-64 border rounded"
            />
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default Contact;
