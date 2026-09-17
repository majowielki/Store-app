import { useNavigate, useParams } from 'react-router-dom';
import { useCreateProductMutation, useGetProductQuery, useGetProductsMetaQuery, useUpdateProductMutation } from '@/api/catalog';
import type { ProductCategory, ProductCompany, ProductPayload } from '@/api/types';
import FormCheckbox from '@/components/FormCheckbox';
import FormInput from '@/components/FormInput';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/hooks/use-toast';

const splitList = (value: FormDataEntryValue | null): string[] =>
  String(value ?? '')
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean);

/** An empty field is sent as null: on update that clears the value instead of keeping the old one. */
const optionalNumber = (value: FormDataEntryValue | null): number | null => {
  const text = String(value ?? '').trim();
  if (!text) return null;
  const parsed = Number(text);
  return Number.isFinite(parsed) ? parsed : null;
};

/** Turns the form fields (all strings) into the typed payload the API validates. */
const toPayload = (fd: FormData): ProductPayload => ({
  title: String(fd.get('title') ?? '').trim(),
  description: String(fd.get('description') ?? '').trim(),
  price: optionalNumber(fd.get('price')) ?? 0,
  salePrice: optionalNumber(fd.get('salePrice')),
  // Free text on the form; the API answers 422 for a value outside its enum
  category: String(fd.get('category') ?? '').trim() as ProductCategory,
  company: String(fd.get('company') ?? '').trim() as ProductCompany,
  newArrival: fd.get('newArrival') === 'on',
  image: String(fd.get('image') ?? '').trim(),
  colors: splitList(fd.get('colors')),
  groups: splitList(fd.get('groups')),
  materials: splitList(fd.get('materials')),
  widthCm: optionalNumber(fd.get('widthCm')),
  heightCm: optionalNumber(fd.get('heightCm')),
  depthCm: optionalNumber(fd.get('depthCm')),
  weightKg: optionalNumber(fd.get('weightKg')),
});

const ProductForm = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const editing = id !== undefined && id !== 'new';
  const { data: product, isLoading } = useGetProductQuery(Number(id), { skip: !editing });
  // Meta is only used for hints; the form still works without it
  const { data: meta } = useGetProductsMetaQuery();
  const [createProduct, { isLoading: creating }] = useCreateProductMutation();
  const [updateProduct, { isLoading: updating }] = useUpdateProductMutation();
  const saving = creating || updating;

  // A refusal (validation 422 with the field messages, or 403 for the demo administrator)
  // has been reported by the error middleware
  const onSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const payload = toPayload(new FormData(e.currentTarget));
    try {
      if (editing) {
        await updateProduct({ id: Number(id), body: { ...payload, isActive: true } }).unwrap();
        toast({ description: 'Product updated.' });
      } else {
        await createProduct(payload).unwrap();
        toast({ description: 'Product created.' });
      }
      navigate('/admin/products');
    } catch {
      // reported
    }
  };

  const hint = (values?: string[]) => (values && values.length ? `e.g. ${values.slice(0, 6).join(', ')}` : undefined);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{editing ? 'Edit' : 'Add'} Product</CardTitle>
      </CardHeader>
      <CardContent>
        {editing && (isLoading || !product) ? (
          <div className="text-sm text-muted-foreground">{isLoading ? 'Loading product…' : 'Product not found.'}</div>
        ) : (
          <form onSubmit={onSubmit} className="grid gap-4 md:grid-cols-2">
            <FormInput type="text" name="title" label="title" defaultValue={product?.title} required minLength={3} maxLength={200} />
            <FormInput type="text" name="company" label="company" defaultValue={product?.company} required placeholder={hint(meta?.companies)} />
            <FormInput type="text" name="category" label="category" defaultValue={product?.category} required placeholder={hint(meta?.categories)} />
            <FormInput type="number" name="price" label="price" defaultValue={product?.price} required min={0.01} step="0.01" />
            <FormInput type="url" name="image" label="image url" defaultValue={product?.image} required />
            <FormInput type="number" name="salePrice" label="sale price" defaultValue={product?.salePrice ?? ''} min={0.01} step="0.01" />
            <FormInput
              type="text"
              name="colors"
              label="colors (comma separated)"
              defaultValue={product?.colors.join(', ') ?? ''}
              required
              placeholder={hint(meta?.colors)}
            />
            <FormInput
              type="text"
              name="groups"
              label="groups (comma separated)"
              defaultValue={product?.groups.join(', ') ?? ''}
              placeholder={hint(meta?.groups)}
            />
            <FormCheckbox name="newArrival" label="new arrival" defaultValue={product?.newArrival ? 'on' : undefined} />
            <div className="md:col-span-2">
              <FormInput
                type="text"
                name="description"
                label="description (10-4000 characters)"
                defaultValue={product?.description}
                required
                minLength={10}
                maxLength={4000}
              />
            </div>
            <FormInput type="number" step="0.01" name="widthCm" label="width (cm)" defaultValue={product?.widthCm ?? ''} />
            <FormInput type="number" step="0.01" name="heightCm" label="height (cm)" defaultValue={product?.heightCm ?? ''} />
            <FormInput type="number" step="0.01" name="depthCm" label="depth (cm)" defaultValue={product?.depthCm ?? ''} />
            <FormInput type="number" step="0.01" name="weightKg" label="weight (kg)" defaultValue={product?.weightKg ?? ''} />
            <FormInput
              type="text"
              name="materials"
              label="materials (comma separated)"
              defaultValue={product?.materials?.join(', ') ?? ''}
            />
            {editing && !product?.isActive && (
              <p className="md:col-span-2 text-sm text-muted-foreground">This product is inactive; saving it puts it back in the shop.</p>
            )}
            <div className="md:col-span-2 flex gap-2">
              <Button type="submit" disabled={saving}>
                {saving ? 'Saving...' : 'Save'}
              </Button>
              <Button type="button" variant="outline" onClick={() => navigate(-1)}>
                Cancel
              </Button>
            </div>
          </form>
        )}
      </CardContent>
    </Card>
  );
};

export default ProductForm;
