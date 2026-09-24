import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { useGetPricingRulesQuery } from '@/api/orders';
import { useAppSelector } from '@/hooks';
import Reveal from './Reveal';
import { Button } from './ui/button';

/** The first-order discount for visitors without an account; signed-in customers do not see it. */
const PromoBand = () => {
  const user = useAppSelector((s) => s.session.user);
  const { data: rules } = useGetPricingRulesQuery();
  if (user) return null;

  return (
    <section className="align-element py-10">
      <Reveal className="relative overflow-hidden rounded-[2rem] bg-brand px-6 py-14 text-brand-foreground sm:px-10 md:px-16 md:py-20">
        <span aria-hidden className="absolute -right-24 -top-24 h-[26rem] w-[26rem] rounded-full border border-current opacity-20" />
        <span aria-hidden className="absolute -right-8 -top-8 h-64 w-64 rounded-full border border-current opacity-20" />
        <span aria-hidden className="absolute -bottom-40 left-1/3 h-80 w-80 rounded-full bg-brand-foreground opacity-[0.07] blur-2xl" />
        <div className="relative max-w-2xl">
          <p className="eyebrow text-current opacity-75">New here?</p>
          <h2 className="display mt-4 text-5xl leading-[0.95] md:text-7xl">
            {rules ? `${rules.firstOrderDiscountPercent}% off` : 'A welcome discount on'} <em>your first order.</em>
          </h2>
          <p className="mt-6 max-w-md opacity-85">
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
        </div>
      </Reveal>
    </section>
  );
};

export default PromoBand;
