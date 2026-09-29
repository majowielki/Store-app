import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetPricingRulesQuery } from '@/api/orders';
import Reveal from '@/components/Reveal';
import { Button } from '@/components/ui/button';
import { usePerks } from '@/content/perks';
import { useAppSelector } from '@/hooks';

/**
 * The one place on the home page for what the shop promises: the welcome discount (for visitors
 * without an account) next to the perks every order gets. The ticker above the header repeats
 * the perks in a line; nothing else on the page does.
 */
const WelcomeBand = () => {
  const user = useAppSelector((s) => s.session.user);
  const { data: rules } = useGetPricingRulesQuery();
  const perks = usePerks();

  return (
    <section className="align-element py-10" aria-labelledby="welcome">
      <Reveal className="relative grid gap-12 overflow-hidden rounded-4xl bg-brand px-6 py-14 text-brand-foreground sm:px-10 md:px-16 md:py-20 lg:grid-cols-2">
        <span aria-hidden className="absolute -right-24 -top-24 h-104 w-104 rounded-full border border-current opacity-20" />
        <span aria-hidden className="absolute -bottom-40 left-1/3 h-80 w-80 rounded-full bg-brand-foreground opacity-[0.07] blur-2xl" />
        <div className="relative">
          <p className="eyebrow text-current">{user ? 'With every order' : 'New here?'}</p>
          <h2 id="welcome" className="display mt-4 text-5xl leading-[0.95] md:text-6xl">
            {user ? (
              <>
                Delivered with <em>care.</em>
              </>
            ) : (
              <>
                {rules ? `${rules.firstOrderDiscountPercent}% off` : 'A welcome discount on'} <em>your first order.</em>
              </>
            )}
          </h2>
          {!user && (
            <>
              <p className="mt-6 max-w-md">
                Create an account and the discount comes off at checkout — no code to remember, no newsletter to sign up for.
              </p>
              <div className="mt-10 flex flex-wrap items-center gap-4">
                <Button asChild size="lg" className="group bg-brand-foreground text-brand hover:bg-brand-foreground/90">
                  <Link to="/register">
                    Create an account
                    <ArrowUpRight className="transition-transform duration-500 ease-smooth group-hover:rotate-45" />
                  </Link>
                </Button>
                <Link to="/login" className="link-underline text-sm font-medium">
                  I already have one
                </Link>
              </div>
            </>
          )}
        </div>
        <ul className="relative grid content-center gap-6 sm:grid-cols-2">
          {perks.map(({ key, icon: Icon, title, description }) => (
            <li key={key} className="flex items-start gap-4">
              <span className="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-brand-foreground/15">
                <Icon className="h-5 w-5" />
              </span>
              <div>
                <h3 className="font-medium">{title}</h3>
                <p className="mt-1 text-sm">{description}</p>
              </div>
            </li>
          ))}
        </ul>
      </Reveal>
    </section>
  );
};

export default WelcomeBand;
