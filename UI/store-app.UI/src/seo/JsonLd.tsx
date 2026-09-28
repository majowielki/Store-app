import type { StructuredData } from './structuredData';

/**
 * Structured data for search engines (schema.org as JSON-LD). A data block, never run, so the
 * content security policy leaves it alone; "<" is escaped so no text in the data can end the element.
 */
const JsonLd = ({ data }: { data: StructuredData | StructuredData[] }) => (
  <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(data).replace(/</g, '\\u003c') }} />
);

export default JsonLd;
