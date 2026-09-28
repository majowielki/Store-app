import { useMemo } from 'react';
import { useGetFinishesQuery } from '@/api/catalog';
import type { Finish } from '@/api/types';

/** The name of a colour the shop does not know as a finish: an older cart or order, or the list still loading. */
const plainName = (key: string) => key.charAt(0).toUpperCase() + key.slice(1).replace(/-/g, ' ');

/** The finishes products are sold in, and a product colour's finish and name by its key. */
export const useFinishes = () => {
  const { data } = useGetFinishesQuery();
  return useMemo(() => {
    const finishes = data ?? [];
    const byKey = new Map(finishes.map((finish) => [finish.key, finish]));
    return {
      finishes,
      finishOf: (key: string): Finish | undefined => byKey.get(key),
      nameOf: (key: string) => byKey.get(key)?.name ?? plainName(key),
    };
  }, [data]);
};
