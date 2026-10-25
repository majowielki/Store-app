import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetProductsQuery } from '@/api/catalog';
import AmbientLoop from '@/components/cinematic/AmbientLoop';
import SplitText from '@/components/cinematic/SplitText';
import Reveal from '@/components/Reveal';
import { materials, type Material } from '@/content/materials';
import { cn } from '@/lib/utils';
import { breadcrumbData, JsonLd, PageMeta } from '@/seo';
import { countOf } from '@/utils';

const MaterialSection = ({ material, flipped }: { material: Material; flipped: boolean }) => {
  const { data } = useGetProductsQuery({ search: material.search, pageSize: 1 });
  return (
    <section id={material.key} aria-labelledby={`${material.key}-title`} className="scroll-mt-28 border-t py-16 md:py-24">
      <div className="grid gap-10 lg:grid-cols-12 lg:items-center lg:gap-16">
        <Reveal className={cn('lg:col-span-7', flipped && 'lg:order-last')}>
          <AmbientLoop poster={material.poster} video={material.video} flag="materialLoops" className="aspect-16/10 rounded-4xl" />
        </Reveal>
        <div className="lg:col-span-5">
          <SplitText as="h2" id={`${material.key}-title`} className="display text-5xl md:text-6xl">
            {material.name}
          </SplitText>
          <p className="mt-6 text-lg leading-relaxed text-muted-foreground">{material.about}</p>
          <h3 className="eyebrow mt-10">Looking after it</h3>
          <ul className="mt-4 grid gap-3 border-t pt-4">
            {material.care.map((habit) => (
              <li key={habit} className="leading-relaxed">
                {habit}
              </li>
            ))}
          </ul>
          <div className="mt-8 flex flex-wrap items-center gap-x-6 gap-y-3 text-sm font-medium">
            <Link to={`/products?search=${encodeURIComponent(material.search)}`} className="group inline-flex items-center gap-2">
              <span className="link-underline">
                {data ? `See the ${countOf(data.totalCount, 'piece')} in ${material.name.toLowerCase()}` : `See the pieces in ${material.name.toLowerCase()}`}
              </span>
              <ArrowUpRight className="h-4 w-4 transition-transform duration-300 group-hover:rotate-45" aria-hidden />
            </Link>
            {material.article && (
              <Link to={`/journal/${material.article.slug}`} className="group inline-flex items-center gap-2 text-muted-foreground hover:text-foreground">
                <span className="link-underline">{material.article.title}</span>
                <ArrowUpRight className="h-4 w-4 transition-transform duration-300 group-hover:rotate-45" aria-hidden />
              </Link>
            )}
          </div>
        </div>
      </div>
    </section>
  );
};

/**
 * What the shop's pieces are made of: each material as a close look with the morning light moving
 * across it, what it is, where the shop uses it, how to look after it, and the way to its pieces.
 */
const Materials = () => (
  <article>
    <PageMeta title="Materials" description="The oak, walnut, linen, bouclé, travertine, rattan and stoneware our pieces are made of - and how to look after them." />
    <JsonLd data={breadcrumbData([{ name: 'Materials', path: '/materials' }])} />
    <header className="max-w-3xl">
      <p className="eyebrow animate-fade-up">Inspiration</p>
      <SplitText as="h1" shown className="display mt-4 text-[clamp(3rem,7vw,6rem)] leading-[0.95]">
        What our pieces are made of
      </SplitText>
      <p className="mt-6 max-w-xl animate-fade-up text-lg leading-relaxed text-muted-foreground [animation-delay:200ms]">
        Seven materials, looked at closely: what each one is, where you will find it in the shop, and the few habits that keep it
        good for years.
      </p>
      <nav aria-label="Materials on this page" className="mt-10 animate-fade-up [animation-delay:300ms]">
        <ul className="flex flex-wrap gap-2">
          {materials.map((material) => (
            <li key={material.key}>
              <a
                href={`#${material.key}`}
                className="inline-flex rounded-full border px-4 py-2 text-sm transition-colors hover:border-foreground hover:bg-foreground hover:text-background"
              >
                {material.name}
              </a>
            </li>
          ))}
        </ul>
      </nav>
    </header>
    <div className="mt-16">
      {materials.map((material, index) => (
        <MaterialSection key={material.key} material={material} flipped={index % 2 === 1} />
      ))}
    </div>
  </article>
);

export default Materials;
