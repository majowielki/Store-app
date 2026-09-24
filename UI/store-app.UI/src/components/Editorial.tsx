import { Link } from 'react-router-dom';
import { ArrowRight, Star } from 'lucide-react';
import hero2 from '@/assets/hero2.webp';
import { useParallax } from '@/hooks/use-parallax';
import Reveal from './Reveal';

const principles = [
  { title: 'Honest materials', text: 'Solid wood, natural fabrics and metals that age well — nothing that looks tired after a season.' },
  { title: 'Delivered with care', text: 'Most orders are at your door within 48 hours, carried in and unwrapped where you want them.' },
  { title: 'Easy to live with', text: 'Thirty days to change your mind and two years of warranty on every piece.' },
];

/** The shop's point of view: a large picture drifting with the scroll next to three principles. */
const Editorial = () => {
  const imageRef = useParallax<HTMLImageElement>(36);
  return (
    <section className="align-element grid items-center gap-16 py-20 md:py-28 lg:grid-cols-2 lg:gap-24">
      <Reveal className="relative">
        <div className="aspect-[4/5] overflow-hidden rounded-[2rem] bg-muted">
          <img ref={imageRef} src={hero2} alt="A round oak dining table under paper pendant lamps" loading="lazy" className="h-full w-full scale-[1.12] object-cover" />
        </div>
        <div className="absolute -bottom-6 right-4 rounded-2xl border bg-background/90 p-5 shadow-xl backdrop-blur md:-right-8">
          <div className="flex gap-0.5 text-brand" aria-hidden>
            {Array.from({ length: 5 }, (_, i) => (
              <Star key={i} className="h-4 w-4 fill-current" />
            ))}
          </div>
          <p className="display mt-2 text-3xl">4.9 / 5</p>
          <p className="text-xs text-muted-foreground">from 10,000+ happy homes</p>
        </div>
      </Reveal>

      <div>
        <Reveal>
          <p className="eyebrow">Our approach</p>
          <h2 className="display mt-3 text-4xl leading-[1.05] md:text-6xl">
            Designed to be <em>lived with</em>, not just looked at.
          </h2>
        </Reveal>
        <ol className="mt-12 divide-y border-y">
          {principles.map((principle, index) => (
            <Reveal as="li" key={principle.title} delay={index * 100} className="grid grid-cols-[3.5rem_1fr] gap-2 py-6">
              <span className="display text-2xl text-brand">0{index + 1}</span>
              <div>
                <h3 className="font-medium">{principle.title}</h3>
                <p className="mt-1.5 text-sm leading-relaxed text-muted-foreground">{principle.text}</p>
              </div>
            </Reveal>
          ))}
        </ol>
        <Reveal delay={200}>
          <Link to="/about" className="group mt-10 inline-flex items-center gap-2 text-sm font-medium">
            <span className="link-underline">Read our story</span>
            <ArrowRight className="h-4 w-4 transition-transform duration-300 group-hover:translate-x-1" />
          </Link>
        </Reveal>
      </div>
    </section>
  );
};

export default Editorial;
