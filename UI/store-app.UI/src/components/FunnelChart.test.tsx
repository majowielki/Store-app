import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import FunnelChart from './FunnelChart';

describe('FunnelChart', () => {
  it('gives every stage its count and the share of the stage before', () => {
    render(
      <FunnelChart
        stages={[
          { stage: 'productViewed', count: 1250 },
          { stage: 'addedToBag', count: 250 },
          { stage: 'orderPlaced', count: 10 },
        ]}
      />,
    );

    const stages = within(screen.getByRole('list', { name: 'Purchase funnel' })).getAllByRole('listitem');
    expect(stages).toHaveLength(3);
    expect(stages[0]).toHaveTextContent('Product views1,250');
    expect(stages[1]).toHaveTextContent('Added to the bag250');
    expect(stages[1]).toHaveTextContent('20% of the views');
    expect(stages[2]).toHaveTextContent('Orders placed10');
    expect(stages[2]).toHaveTextContent('4% of the bag additions');
  });

  it('says so when a stage before had nothing yet', () => {
    render(
      <FunnelChart
        stages={[
          { stage: 'productViewed', count: 0 },
          { stage: 'addedToBag', count: 0 },
          { stage: 'orderPlaced', count: 0 },
        ]}
      />,
    );

    expect(screen.getByText('No views yet')).toBeInTheDocument();
    expect(screen.getByText('No bag additions yet')).toBeInTheDocument();
  });
});
