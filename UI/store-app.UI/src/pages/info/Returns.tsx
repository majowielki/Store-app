import { Link } from 'react-router-dom';
import InfoPage, { InfoSection } from './InfoPage';

/** Returns within 30 days and complaints under the warranty. */
const Returns = () => (
  <InfoPage
    eyebrow="Returns & complaints"
    title="Changed your mind?"
    lead="You have 30 days from delivery to return anything you have not used, without giving a reason."
  >
    <InfoSection title="Returning a piece">
      <ol className="list-decimal space-y-2 pl-6">
        <li>
          Tell us from the <Link to="/contact" className="link-underline">contact page</Link> which order and which pieces
          you are returning.
        </li>
        <li>Pack them the way they came, or as well as you can - we collect large pieces ourselves.</li>
        <li>We refund the price and the original delivery cost within 14 days of receiving the return.</li>
      </ol>
    </InfoSection>
    <InfoSection title="Something is wrong">
      <p>
        Every piece comes with a two-year warranty. If something arrives damaged or fails in normal use, send us a photo
        and the order number; we repair it, replace it or refund it, and collect the faulty piece at our cost.
      </p>
    </InfoSection>
    <InfoSection title="What cannot be returned">
      <p>Pieces made or cut to your measurements, and textiles that have been used or washed.</p>
    </InfoSection>
  </InfoPage>
);

export default Returns;
