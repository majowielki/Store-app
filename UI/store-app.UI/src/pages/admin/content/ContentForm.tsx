import { useState, type ComponentType, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useGetProductsMetaQuery } from '@/api/catalog';
import {
  useCreateContentMutation,
  useGetContentEntryAdminQuery,
  useUpdateContentMutation,
  type ContentKind,
  type ContentPayload,
} from '@/api/content';
import type { ArticlePayload, CollectionPayload, LookbookPayload, MakerPayload, ProductCompany } from '@/api/types';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/hooks/use-toast';
import { contentKinds, emptyPayload, isContentKind, slugify, toPayload } from './contentKinds';
import { Field, ImageField, MarkdownField, PublishedField, TextField } from './fields';
import HotspotEditor from './HotspotEditor';
import ProductPicker from './ProductPicker';

/** Creates or edits one entry of any kind: /admin/content/:kind/new or /admin/content/:kind/:id. */
const ContentForm = () => {
  const { kind, id } = useParams<{ kind: string; id: string }>();
  if (!isContentKind(kind)) return <p className="p-6">Unknown kind of content.</p>;
  return <EntryLoader key={`${kind}-${id ?? 'new'}`} kind={kind} id={id ? Number(id) : null} />;
};

const EntryLoader = ({ kind, id }: { kind: ContentKind; id: number | null }) => {
  const { data: entry, isLoading } = useGetContentEntryAdminQuery({ kind, id: id ?? 0 }, { skip: id === null });
  if (id !== null && isLoading) return <p className="p-6">Loading…</p>;
  if (id !== null && !entry) return <p className="p-6">This entry does not exist.</p>;
  const initial = entry ? toPayload(kind, entry) : emptyPayload(kind);
  if (kind === 'makers' && 'name' in initial)
    return <EntryForm kind={kind} id={id} initial={initial} Fields={MakerFields} titleChange={(name) => ({ name })} />;
  if (kind === 'articles' && 'excerpt' in initial)
    return <EntryForm kind={kind} id={id} initial={initial} Fields={ArticleFields} titleChange={(title) => ({ title })} />;
  if (kind === 'lookbooks' && 'hotspots' in initial)
    return <EntryForm kind={kind} id={id} initial={initial} Fields={LookbookFields} titleChange={(title) => ({ title })} />;
  if (kind === 'collections' && 'body' in initial && 'summary' in initial)
    return <EntryForm kind={kind} id={id} initial={initial} Fields={CollectionFields} titleChange={(title) => ({ title })} />;
  return <p className="p-6">The entry has an unexpected shape.</p>;
};

interface EntryFormProps<T extends ContentPayload<ContentKind>> {
  kind: ContentKind;
  id: number | null;
  initial: T;
  Fields: ComponentType<FieldsProps<T>>;
  titleChange: (value: string) => Partial<T>;
}

const EntryForm = <T extends ContentPayload<ContentKind>,>({ kind, id, initial, Fields, titleChange }: EntryFormProps<T>) => {
  const config = contentKinds[kind];
  const navigate = useNavigate();
  const [draft, setDraft] = useState(initial);
  // Until the slug is typed by hand, it follows the title
  const [slugTouched, setSlugTouched] = useState(id !== null);
  const [createContent, { isLoading: creating }] = useCreateContentMutation();
  const [updateContent, { isLoading: updating }] = useUpdateContentMutation();

  const set = (change: Partial<T>) => setDraft((current) => ({ ...current, ...change }));
  const setTitle = (value: string) => setDraft((current) => ({ ...current, ...titleChange(value), ...(slugTouched ? {} : { slug: slugify(value) }) }));

  const pointsWithoutProduct = 'hotspots' in draft ? draft.hotspots.filter((h) => !h.productSlug).length : 0;

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (pointsWithoutProduct > 0) {
      toast({ variant: 'destructive', description: `Choose a product for every point (${pointsWithoutProduct} without one).` });
      return;
    }
    try {
      if (id === null) {
        await createContent({ kind, body: draft }).unwrap();
        toast({ description: `The ${config.singular} is saved.` });
      } else {
        await updateContent({ kind, id, body: draft }).unwrap();
        toast({ description: 'Changes saved.' });
      }
      navigate(`/admin/content/${kind}`);
    } catch {
      // reported by the error middleware (403 for the demo administrator, 409 for a taken slug)
    }
  };

  const slugField = (
    <TextField
      label="Address (slug)"
      value={draft.slug}
      onChange={(value) => {
        setSlugTouched(true);
        setDraft((current) => ({ ...current, slug: value }));
      }}
      required
      maxLength={120}
      hint={
        <>
          The page lives at <code className="font-mono">{config.publicPath(draft.slug || '…')}</code>. Lowercase words joined by dashes.
        </>
      }
    />
  );

  return (
    <Card>
      <CardHeader>
        <CardTitle>{id === null ? `Add a ${config.singular}` : `Edit the ${config.singular}`}</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="grid gap-6">
          <Fields draft={draft} set={set} setTitle={setTitle} slugField={slugField} />
          <PublishedField value={draft.isPublished} onChange={(isPublished) => setDraft((current) => ({ ...current, isPublished }))} />
          <div className="flex gap-3">
            <Button type="submit" disabled={creating || updating}>
              {creating || updating ? 'Saving…' : 'Save'}
            </Button>
            <Button asChild variant="outline">
              <Link to={`/admin/content/${kind}`}>Cancel</Link>
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
};

interface FieldsProps<T> {
  draft: T;
  set: (change: Partial<T>) => void;
  setTitle: (value: string) => void;
  slugField: React.ReactNode;
}

const MakerFields = ({ draft, set, setTitle, slugField }: FieldsProps<MakerPayload>) => {
  const { data: meta } = useGetProductsMetaQuery();
  const companies = (meta?.companies ?? []).filter((c) => c !== 'all');
  return (
    <>
      <div className="grid gap-6 md:grid-cols-2">
        <TextField label="Name" value={draft.name} onChange={(value) => setTitle(value)} required maxLength={160} />
        {slugField}
        <Field label="Company in the catalogue" hint="The maker's page lists this company's products.">
          <select
            value={draft.company}
            onChange={(e) => set({ company: e.target.value as ProductCompany })}
            className="h-11 rounded-lg border border-input bg-card px-3 text-sm"
          >
            {companies.map((company) => (
              <option key={company} value={company}>
                {company}
              </option>
            ))}
          </select>
        </Field>
        <TextField label="Tagline" value={draft.tagline} onChange={(tagline) => set({ tagline })} maxLength={200} />
        <TextField label="Location" value={draft.location} onChange={(location) => set({ location })} maxLength={200} />
        <TextField
          label="Founded in"
          type="number"
          value={draft.foundedYear?.toString() ?? ''}
          onChange={(value) => set({ foundedYear: value ? Number(value) : null })}
        />
      </div>
      <ImageField label="Cover picture" value={draft.coverImage} onChange={(coverImage) => set({ coverImage })} />
      <MarkdownField label="Story" value={draft.story} onChange={(story) => set({ story })} />
    </>
  );
};

const CollectionFields = ({ draft, set, setTitle, slugField }: FieldsProps<CollectionPayload>) => (
  <>
    <div className="grid gap-6 md:grid-cols-2">
      <TextField label="Title" value={draft.title} onChange={(value) => setTitle(value)} required maxLength={160} />
      {slugField}
      <TextField label="Summary" value={draft.summary} onChange={(summary) => set({ summary })} multiline maxLength={500} className="md:col-span-2" />
      <TextField label="Order in lists" type="number" value={String(draft.sortOrder)} onChange={(value) => set({ sortOrder: Number(value) || 0 })} />
    </div>
    <ImageField label="Cover picture" value={draft.coverImage} onChange={(coverImage) => set({ coverImage })} />
    <MarkdownField label="Text" value={draft.body} onChange={(body) => set({ body })} />
    <ProductPicker value={draft.productSlugs} onChange={(productSlugs) => set({ productSlugs })} />
  </>
);

const ArticleFields = ({ draft, set, setTitle, slugField }: FieldsProps<ArticlePayload>) => (
  <>
    <div className="grid gap-6 md:grid-cols-2">
      <TextField label="Title" value={draft.title} onChange={(value) => setTitle(value)} required maxLength={160} />
      {slugField}
      <TextField label="Excerpt" value={draft.excerpt} onChange={(excerpt) => set({ excerpt })} multiline maxLength={500} className="md:col-span-2" />
      <TextField label="Author" value={draft.author} onChange={(author) => set({ author })} maxLength={200} />
      <TextField
        label="Date"
        type="datetime-local"
        value={draft.publishedAt ? draft.publishedAt.slice(0, 16) : ''}
        onChange={(value) => set({ publishedAt: value ? new Date(value).toISOString() : null })}
      />
    </div>
    <ImageField label="Cover picture" value={draft.coverImage} onChange={(coverImage) => set({ coverImage })} />
    <MarkdownField label="Article" value={draft.body} onChange={(body) => set({ body })} required />
    <ProductPicker value={draft.productSlugs} onChange={(productSlugs) => set({ productSlugs })} />
  </>
);

const LookbookFields = ({ draft, set, setTitle, slugField }: FieldsProps<LookbookPayload>) => (
  <>
    <div className="grid gap-6 md:grid-cols-2">
      <TextField label="Title" value={draft.title} onChange={(value) => setTitle(value)} required maxLength={160} />
      {slugField}
      <TextField label="Summary" value={draft.summary} onChange={(summary) => set({ summary })} multiline maxLength={500} className="md:col-span-2" />
      <TextField label="Order in lists" type="number" value={String(draft.sortOrder)} onChange={(value) => set({ sortOrder: Number(value) || 0 })} />
    </div>
    <ImageField label="Picture of the room" value={draft.image} onChange={(image) => set({ image })} />
    <HotspotEditor image={draft.image} hotspots={draft.hotspots} onChange={(hotspots) => set({ hotspots })} />
  </>
);

export default ContentForm;
