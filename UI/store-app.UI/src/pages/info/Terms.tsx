import { Link } from 'react-router-dom';
import InfoPage, { DemoNote, InfoSection } from './InfoPage';

/** The terms of the shop, short and plain. */
const Terms = () => (
  <InfoPage eyebrow="Terms & conditions" title="The terms of the shop" lead="The rules that apply when you use the shop and place an order.">
    <DemoNote />
    <InfoSection title="Orders">
      <p>
        An order is placed when you confirm it at checkout and is accepted when we send the confirmation. Prices include VAT
        and are the ones shown in the bag at that moment; the first-order discount and the delivery cost are shown before you
        confirm.
      </p>
    </InfoSection>
    <InfoSection title="Delivery and returns">
      <p>
        Delivery times and costs are described on the <Link to="/shipping" className="link-underline">shipping page</Link>,
        returns and complaints on the <Link to="/returns" className="link-underline">returns page</Link>. Your statutory
        rights as a consumer are not affected by anything here.
      </p>
    </InfoSection>
    <InfoSection title="Accounts">
      <p>
        Keep your password to yourself. We may close an account used to abuse the shop, for example to post content that
        breaks the law or the rights of others.
      </p>
    </InfoSection>
  </InfoPage>
);

export default Terms;
