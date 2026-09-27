import { useGetPricingRulesQuery } from '@/api/orders';
import { formatAsDollars } from '@/utils';
import InfoPage, { DemoNote, InfoSection } from './InfoPage';

/**
 * Delivery and payment. The amounts are the order service's own rules (the ones the bag and the
 * checkout apply), so this page cannot quote a price the checkout would not charge.
 */
const Shipping = () => {
  const { data: rules } = useGetPricingRulesQuery();

  return (
    <InfoPage
      eyebrow="Shipping & payments"
      title="Delivery to your door"
      lead="Every piece is delivered by our own couriers or a trusted partner, carried to the room you choose and unpacked if you like."
    >
      <InfoSection title="What delivery costs">
        {rules ? (
          <dl className="grid gap-px overflow-hidden rounded-2xl border bg-border sm:grid-cols-2">
            <div className="bg-background p-5">
              <dt className="text-sm text-muted-foreground">Orders from {formatAsDollars(rules.freeDeliveryThreshold)}</dt>
              <dd className="display mt-1 text-3xl">Free</dd>
            </div>
            <div className="bg-background p-5">
              <dt className="text-sm text-muted-foreground">Smaller orders</dt>
              <dd className="display mt-1 text-3xl">{formatAsDollars(rules.deliveryFee)}</dd>
            </div>
          </dl>
        ) : (
          <p>Delivery is free above a set order value; below it a flat fee applies. The bag shows both before you order.</p>
        )}
        {rules && (
          <p>
            Your first order is also {rules.firstOrderDiscountPercent}% cheaper - the discount is taken off in the bag, before
            delivery is worked out.
          </p>
        )}
      </InfoSection>
      <InfoSection title="When it arrives">
        <p>
          Most pieces leave our warehouse the next working day and reach you within two to four working days. Large pieces
          are delivered by appointment: the courier calls the day before with a two-hour window.
        </p>
      </InfoSection>
      <InfoSection title="Paying">
        <p>You pay when you place the order, by card. Your card details go to the payment provider and never to us.</p>
        <DemoNote />
      </InfoSection>
    </InfoPage>
  );
};

export default Shipping;
