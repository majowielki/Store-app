import { Card, CardContent } from '@/components/ui/card';

const Contact = () => {
  return (
    <div className="grid gap-6 md:grid-cols-2">
      <Card>
        <CardContent className="p-6">
          <h2 className="text-xl font-semibold">Contact</h2>
          <p className="text-muted-foreground mt-2">Have questions? Send us a message.</p>
          <form className="mt-4 grid gap-3">
            <input className="border rounded px-3 py-2" placeholder="Full name" />
            <input className="border rounded px-3 py-2" placeholder="Email" type="email" />
            <textarea className="border rounded px-3 py-2" placeholder="Message" rows={5} />
            <button className="bg-primary text-primary-foreground px-4 py-2 rounded" type="button">Send</button>
          </form>
        </CardContent>
      </Card>
      <Card>
        <CardContent className="p-6">
          <h3 className="font-semibold">Company details</h3>
          <p className="text-sm text-muted-foreground mt-2">1 Example St, 00-000 City</p>
          <p className="text-sm text-muted-foreground">+48 000 000 000</p>
          <p className="text-sm text-muted-foreground">kontakt@sklep.pl</p>
          <div className="mt-4">
            <iframe
              title="Mapa"
              src="https://maps.google.com/maps?q=Warsaw&t=&z=11&ie=UTF8&iwloc=&output=embed"
              className="w-full h-64 border rounded"
            />
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default Contact;
