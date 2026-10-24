import SplitText from './cinematic/SplitText';

interface SectionTitleProps {
  text: string;
  /** A small line above the title. */
  eyebrow?: string;
}

/** The title of a page of the shop, in the display serif over a hairline. */
const SectionTitle = ({ text, eyebrow }: SectionTitleProps) => {
  return (
    <header className="animate-fade-up border-b pb-8">
      {eyebrow && <p className="eyebrow mb-3">{eyebrow}</p>}
      {/* The first letter is raised here: ::first-letter does not reach into the split words */}
      <SplitText as="h1" className="display text-5xl md:text-6xl">
        {text.charAt(0).toUpperCase() + text.slice(1)}
      </SplitText>
    </header>
  );
};

export default SectionTitle;
