import { Card, CardContent } from "@/components/ui/card";
import SectionTitle from "@/components/SectionTitle";
import { Leaf, ShieldCheck, Truck, Sparkles } from "lucide-react";
import hero1 from "@/assets/hero1.webp";
import hero2 from "@/assets/hero2.webp";

const About = () => {
  return (
    <section className="space-y-10">
      <SectionTitle text="About us" />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
        {/* Copy & features */}
        <div>
          <h1 className="font-bold text-4xl tracking-tight sm:text-5xl leading-tight">
            We design comfort that looks as good as it feels
          </h1>
          <p className="mt-6 max-w-xl text-lg leading-8 text-muted-foreground">
            From concept to delivery — we obsess over the details. Our products
            blend function with aesthetics so everyday life simply feels better.
          </p>

          <ul className="mt-8 grid grid-cols-1 sm:grid-cols-2 gap-4">
            <li className="flex items-start gap-3">
              <span className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-secondary text-secondary-foreground">
                <Leaf className="h-5 w-5" />
              </span>
              <div>
                <p className="font-medium">Sustainable materials</p>
                <p className="text-sm text-muted-foreground">
                  We choose environmentally friendly resources.
                </p>
              </div>
            </li>
            <li className="flex items-start gap-3">
              <span className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-secondary text-secondary-foreground">
                <ShieldCheck className="h-5 w-5" />
              </span>
              <div>
                <p className="font-medium">Trusted quality</p>
                <p className="text-sm text-muted-foreground">
                  Craftsmanship and attention to detail.
                </p>
              </div>
            </li>
            <li className="flex items-start gap-3">
              <span className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-secondary text-secondary-foreground">
                <Truck className="h-5 w-5" />
              </span>
              <div>
                <p className="font-medium">Fast delivery</p>
                <p className="text-sm text-muted-foreground">
                  Fast delivery and hassle-free returns.
                </p>
              </div>
            </li>
            <li className="flex items-start gap-3">
              <span className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-secondary text-secondary-foreground">
                <Sparkles className="h-5 w-5" />
              </span>
              <div>
                <p className="font-medium">Timeless design</p>
                <p className="text-sm text-muted-foreground">
                  Forms that never go out of style.
                </p>
              </div>
            </li>
          </ul>

      <div className="mt-8 grid grid-cols-1 md:grid-cols-3 gap-6">
      <Card>
              <CardContent className="pt-6 text-center">
                <p className="text-3xl font-bold tracking-tight">10k+</p>
        <p className="text-sm text-muted-foreground">happy customers</p>
              </CardContent>
            </Card>
      <Card>
              <CardContent className="pt-6 text-center">
                <p className="text-3xl font-bold tracking-tight">4.9/5</p>
        <p className="text-sm text-muted-foreground">average rating</p>
              </CardContent>
            </Card>
      <Card>
              <CardContent className="pt-6 text-center">
    <p className="text-3xl font-bold tracking-tight">48h</p>
  <p className="text-sm text-muted-foreground">delivery time</p>
              </CardContent>
            </Card>
          </div>
        </div>

        {/* Visual collage */}
        <div>
          {/* Simple grid collage for all screens */}
          <div className="relative rounded-2xl p-3 bg-gradient-to-br from-primary/10 via-accent/20 to-transparent">
            <div className="grid grid-cols-2 gap-3">
              <img
                src={hero1}
                alt="Cozy armchair in a modern interior"
                className="col-span-1 aspect-[3/4] w-full rounded-xl object-cover shadow-sm"
                loading="lazy"
              />
              <img
                src={hero2}
                alt="Material details and finish"
                className="col-span-1 aspect-[3/4] w-full rounded-xl object-cover shadow-sm"
                loading="lazy"
              />
              <div className="col-span-2">
                <div className="flex flex-wrap items-center gap-2 sm:gap-3">
                  <span className="inline-flex items-center gap-2 rounded-full bg-secondary px-3 py-1.5 text-xs sm:text-sm text-secondary-foreground">
                    <Leaf className="h-4 w-4" aria-hidden />
                    Ethically sourced
                  </span>
                  <span className="inline-flex items-center gap-2 rounded-full bg-secondary px-3 py-1.5 text-xs sm:text-sm text-secondary-foreground">
                    <ShieldCheck className="h-4 w-4" aria-hidden />
                    2-year warranty
                  </span>
                  <span className="inline-flex items-center gap-2 rounded-full bg-secondary px-3 py-1.5 text-xs sm:text-sm text-secondary-foreground">
                    <Truck className="h-4 w-4" aria-hidden />
                    Free returns
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Enhanced layered collage removed for xl screens */}
        </div>
      </div>
    </section>
  );
};
export default About;
