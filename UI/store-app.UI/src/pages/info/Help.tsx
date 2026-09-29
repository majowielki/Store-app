import { Link } from 'react-router-dom';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';
import InfoPage from './InfoPage';

const questions = [
  {
    q: 'Do I need an account to order?',
    a: 'You can fill your bag as a guest; it stays in this browser. To order, sign in or create an account - the bag comes with you, and your orders and address are kept for next time.',
  },
  {
    q: 'How long does delivery take?',
    a: (
      <>
        Most pieces leave our warehouse the next working day and arrive within two to four working days. Delivery costs and
        the threshold for free delivery are on the <Link to="/shipping" className="link-underline">shipping page</Link>.
      </>
    ),
  },
  {
    q: 'Can I return something?',
    a: (
      <>
        Yes, within 30 days and without giving a reason, as long as the piece is unused. See{' '}
        <Link to="/returns" className="link-underline">returns and complaints</Link> for how it works.
      </>
    ),
  },
  {
    q: 'Why is there only one colour of most pieces?',
    a: 'Most of our furniture is solid wood, finished the way the wood looks best. Where a choice makes sense - textiles, a few upholstered pieces - the product page lets you pick the colour.',
  },
  {
    q: 'What is the welcome discount?',
    a: 'Your first order is cheaper by the percentage shown in the bag; it is taken off automatically, with no code to type.',
  },
  {
    q: 'What are the demo accounts on the sign-in page?',
    a: 'This shop is a portfolio project. The demo customer and demo administrator let anyone look around without registering; the administrator can see the panel but cannot change anything.',
  },
];

/** Answers to the questions customers ask most, in an accordion. */
const Help = () => (
  <InfoPage
    eyebrow="Help"
    title="How can we help?"
    lead="The questions we are asked most often. If yours is not here, write to us from the contact page - we answer within a working day."
  >
    <Accordion type="single" collapsible className="border-t">
      {questions.map(({ q, a }, index) => (
        <AccordionItem key={q} value={`q-${index}`}>
          <AccordionTrigger headingLevel={2} className="text-left text-lg">{q}</AccordionTrigger>
          <AccordionContent className="text-base leading-relaxed text-muted-foreground">{a}</AccordionContent>
        </AccordionItem>
      ))}
    </Accordion>
    <p className="text-sm text-muted-foreground">
      Still stuck?{' '}
      <Link to="/contact" className="link-underline font-medium text-foreground">
        Contact us
      </Link>
      .
    </p>
  </InfoPage>
);

export default Help;
