import type { Meta, StoryObj } from '@storybook/react-vite';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from './accordion';

const questions = [
  { q: 'How long does delivery take?', a: 'Most pieces leave the warehouse the next working day and arrive within two to four working days.' },
  { q: 'Can I return something?', a: 'Yes, within 30 days and without giving a reason, as long as the piece is unused.' },
  { q: 'What is the welcome discount?', a: 'Your first order is cheaper by the percentage shown in the bag, with no code to type.' },
];

/** Questions that open one at a time, as on the help page; the first one is open. */
const meta = {
  title: 'Primitives/Accordion',
  component: Accordion,
  args: { type: 'single', collapsible: true, defaultValue: 'q-0' },
  render: (args) => (
    <Accordion {...args} className="w-[36rem] border-t">
      {questions.map(({ q, a }, index) => (
        <AccordionItem key={q} value={`q-${index}`}>
          <AccordionTrigger headingLevel={2} className="text-left text-lg">
            {q}
          </AccordionTrigger>
          <AccordionContent className="text-base">{a}</AccordionContent>
        </AccordionItem>
      ))}
    </Accordion>
  ),
} satisfies Meta<typeof Accordion>;

export default meta;
type Story = StoryObj<typeof meta>;

export const HelpQuestions: Story = {};
