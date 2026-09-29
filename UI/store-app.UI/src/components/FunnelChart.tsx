import type { Funnel, FunnelStage } from '@/api/types';
import { SHOP_LOCALE } from '@/utils';

const STAGE_LABELS: Record<FunnelStage, string> = {
  productViewed: 'Product views',
  addedToBag: 'Added to the bag',
  orderPlaced: 'Orders placed',
};

/** What a stage is a share of, in "12% of the views": the stage before it. */
const STAGE_NOUNS: Record<FunnelStage, string> = {
  productViewed: 'views',
  addedToBag: 'bag additions',
  orderPlaced: 'orders',
};

/** The narrowest bar a stage reached at least once gets, so a small count still shows. */
const MIN_BAR_PX = 2;

const count = new Intl.NumberFormat(SHOP_LOCALE);
const percent = new Intl.NumberFormat(SHOP_LOCALE, { style: 'percent', maximumFractionDigits: 1 });

/**
 * The purchase funnel as bars from one baseline, each as long as its stage's share of the first,
 * with the count and the share of the stage before in words - the numbers are text, the bars only
 * show the narrowing.
 */
const FunnelChart = ({ stages }: { stages: Funnel['stages'] }) => {
  const first = stages[0]?.count ?? 0;
  return (
    <ol className="grid gap-4" aria-label="Purchase funnel">
      {stages.map((stage, index) => {
        const previous = index > 0 ? stages[index - 1] : undefined;
        const share = first > 0 ? stage.count / first : 0;
        return (
          <li key={stage.stage} className="grid gap-1.5">
            <div className="flex items-baseline justify-between gap-3 text-sm">
              <span>{STAGE_LABELS[stage.stage]}</span>
              <span className="font-semibold tabular-nums">{count.format(stage.count)}</span>
            </div>
            <div aria-hidden="true" className="h-5">
              {stage.count > 0 && (
                <div className="h-full rounded-r-[4px] bg-chart-1" style={{ width: `max(${MIN_BAR_PX}px, ${share * 100}%)` }} />
              )}
            </div>
            {previous && (
              <p className="text-xs text-muted-foreground">
                {previous.count > 0 ? `${percent.format(stage.count / previous.count)} of the ${STAGE_NOUNS[previous.stage]}` : `No ${STAGE_NOUNS[previous.stage]} yet`}
              </p>
            )}
          </li>
        );
      })}
    </ol>
  );
};

export default FunnelChart;
