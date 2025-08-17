import { infoTiles } from '@/content/infoTiles';
import { ShieldCheck, Truck, RotateCcw, Percent } from 'lucide-react';

const iconMap = {
  'shield-check': ShieldCheck,
  'truck': Truck,
  'rotate-ccw': RotateCcw,
  'percent': Percent,
} as const;
type IconKey = keyof typeof iconMap;

const InfoTiles = () => {
  return (
    <section className="py-12">
      <div className="align-element grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {infoTiles.map((t) => {
          const key = (t.icon ?? 'shield-check') as IconKey;
          const Icon = iconMap[key] ?? ShieldCheck;
          return (
            <div key={t.title} className="p-6 rounded-md bg-muted/40">
              <div className="flex items-center gap-3">
                <Icon className="h-6 w-6 text-primary" />
                <h3 className="font-semibold">{t.title}</h3>
              </div>
              <p className="text-sm text-muted-foreground mt-2">{t.description}</p>
            </div>
          );
        })}
      </div>
    </section>
  );
};

export default InfoTiles;
