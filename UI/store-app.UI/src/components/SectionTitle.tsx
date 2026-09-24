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
      <h2 className="display text-5xl first-letter:uppercase md:text-6xl">{text}</h2>
    </header>
  );
};

export default SectionTitle;
