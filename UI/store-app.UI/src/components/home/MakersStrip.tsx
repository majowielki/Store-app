import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetMakersQuery } from '@/api/content';
import Reveal from '@/components/Reveal';

/** The workshops behind the catalogue, one portrait card each. */
const MakersStrip = () => {
  const { data: makers } = useGetMakersQuery();
  if (!makers?.length) return null;

  return (
    <section className="align-element py-20 md:py-28" aria-labelledby="makers">
      <Reveal as="header" className="flex flex-wrap items-end justify-between gap-6">
        <div>
          <p className="eyebrow">Who makes it</p>
          <h2 id="makers" className="display mt-3 text-4xl md:text-6xl">
            Five workshops, <em>one standard.</em>
          </h2>
        </div>
        <Link to="/makers" className="group inline-flex items-center gap-2 text-sm font-medium">
          <span className="link-underline">Meet the makers</span>
          <ArrowUpRight className="h-4 w-4 transition-transform duration-300 group-hover:rotate-45" />
        </Link>
      </Reveal>
      <ul className="mt-12 grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-5">
        {makers.map((maker, index) => (
          <Reveal as="li" key={maker.id} delay={index * 70}>
            <Link to={`/makers/${maker.slug}`} className="group relative block aspect-3/4 overflow-hidden rounded-3xl bg-muted">
              <img src={maker.coverImage} alt="" loading="lazy" className="absolute inset-0 h-full w-full object-cover transition-transform [transition-duration:1200ms] ease-smooth group-hover:scale-105" />
              <div className="absolute inset-0 bg-linear-to-t from-black/70 via-black/10 to-transparent" />
              <div className="absolute inset-x-0 bottom-0 p-5 text-white">
                <p className="display text-3xl">{maker.name}</p>
                <p className="mt-1 text-sm leading-snug text-white/80">{maker.tagline}</p>
              </div>
            </Link>
          </Reveal>
        ))}
      </ul>
    </section>
  );
};

export default MakersStrip;
