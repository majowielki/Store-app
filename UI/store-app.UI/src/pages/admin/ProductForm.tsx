import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { customFetch } from '@/utils';
import type { ProductData, ProductsMeta } from '@/utils/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useToast, toast } from '@/hooks/use-toast';
import FormInput from '@/components/FormInput';
import FormCheckbox from '@/components/FormCheckbox';

/** Body of POST /api/products and PUT /api/products/{id} as the catalogue service expects it. */
interface ProductPayload {
  title: string;
  description: string;
  price: number;
  salePrice?: number;
  category: string;
  company: string;
  newArrival: boolean;
  image: string;
  colors: string[];
  groups?: string[];
  materials?: string[];
  widthCm?: number;
  heightCm?: number;
  depthCm?: number;
  weightKg?: number;
}

const splitList = (value: FormDataEntryValue | null): string[] =>
  String(value ?? '')
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean);

const optionalNumber = (value: FormDataEntryValue | null): number | undefined => {
  const text = String(value ?? '').trim();
  if (!text) return undefined;
  const parsed = Number(text);
  return Number.isFinite(parsed) ? parsed : undefined;
};

/** Turns the form fields (all strings) into the typed payload the API validates. */
const toPayload = (fd: FormData): ProductPayload => ({
  title: String(fd.get('title') ?? '').trim(),
  description: String(fd.get('description') ?? '').trim(),
  price: optionalNumber(fd.get('price')) ?? 0,
  salePrice: optionalNumber(fd.get('salePrice')),
  category: String(fd.get('category') ?? '').trim(),
  company: String(fd.get('company') ?? '').trim(),
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

interface ApiError {
  response?: {
    status?: number;
    data?: { errors?: Record<string, string[]>; title?: string; message?: string };
  };
}

/** Maps API failures to what the admin should read: permission, validation details or a generic message. */
const describeError = (err: unknown, fallback: string): string => {
  const response = (err as ApiError)?.response;
  if (response?.status === 403) return 'Demo admin is not allowed to perform this action.';
  if (response?.status === 400 && response.data?.errors) {
    const details = Object.entries(response.data.errors)
      .map(([field, messages]) => `${field}: ${messages[0]}`)
      .join('; ');
    return details ? `Please correct: ${details}` : fallback;
  }
  return fallback;
};

const ProductForm = () => {
  useToast();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const editing = !!id && id !== 'new';
  const [product, setProduct] = useState<ProductData | null>(null);
  const [meta, setMeta] = useState<ProductsMeta | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const metaResponse = await customFetch.get<ProductsMeta>('/products/meta');
        setMeta(metaResponse.data);
      } catch {
        // Meta is only used for hints; the form still works without it
      }
      if (editing) {
        const res = await customFetch.get(`/products/${id}`);
        setProduct(res.data.data);
      }
    })();
  }, [editing, id]);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    const payload = toPayload(new FormData(e.target as HTMLFormElement));
    try {
      if (editing) {
        await customFetch.put(`/products/${id}`, payload);
      } else {
        await customFetch.post('/products', payload);
      }
      navigate('/admin/products');
    } catch (err) {
      toast({
        description: describeError(err, editing ? 'Failed to update product.' : 'Failed to create product.'),
        variant: 'destructive',
      });
    } finally {
      setSaving(false);
    }
  };

  const attrs = product?.attributes;
  const hint = (values?: string[]) => (values && values.length ? `e.g. ${values.slice(0, 6).join(', ')}` : undefined);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{editing ? 'Edit' : 'Add'} Product</CardTitle>
      </CardHeader>
      <CardContent>
        {editing && !product ? (
          <div className="text-sm text-muted-foreground">Loading product…</div>
        ) : (
          <form onSubmit={onSubmit} className="grid gap-4 md:grid-cols-2">
            <FormInput type="text" name="title" label="title" defaultValue={attrs?.title} required minLength={3} maxLength={200} />
            <FormInput type="text" name="company" label="company" defaultValue={attrs?.company} required placeholder={hint(meta?.companies)} />
            <FormInput type="text" name="category" label="category" defaultValue={attrs?.category} required placeholder={hint(meta?.categories)} />
            <FormInput type="number" name="price" label="price" defaultValue={attrs?.price} required min={0.01} step="0.01" />
            <FormInput type="url" name="image" label="image url" defaultValue={attrs?.image} required />
            <FormInput type="number" name="salePrice" label="sale price" defaultValue={attrs?.salePrice ?? ''} min={0.01} step="0.01" />
            <FormInput
              type="text"
              name="colors"
              label="colors (comma separated)"
              defaultValue={attrs?.colors?.join(', ') ?? ''}
              required
              placeholder={hint(meta?.colors)}
            />
            <FormInput
              type="text"
              name="groups"
              label="groups (comma separated)"
              defaultValue={attrs?.groups?.join(', ') ?? ''}
              placeholder={hint(meta?.groups)}
            />
            <FormCheckbox name="newArrival" label="new arrival" defaultValue={attrs?.newArrival ? 'on' : undefined} />
            <div className="md:col-span-2">
              <FormInput
                type="text"
                name="description"
                label="description (10-4000 characters)"
                defaultValue={attrs?.description}
                required
                minLength={10}
                maxLength={4000}
              />
            </div>
            <FormInput type="number" step="0.01" name="widthCm" label="width (cm)" defaultValue={attrs?.widthCm ?? ''} />
            <FormInput type="number" step="0.01" name="heightCm" label="height (cm)" defaultValue={attrs?.heightCm ?? ''} />
            <FormInput type="number" step="0.01" name="depthCm" label="depth (cm)" defaultValue={attrs?.depthCm ?? ''} />
            <FormInput type="number" step="0.01" name="weightKg" label="weight (kg)" defaultValue={attrs?.weightKg ?? ''} />
            <FormInput
              type="text"
              name="materials"
              label="materials (comma separated)"
              defaultValue={Array.isArray(attrs?.materials) ? attrs.materials.join(', ') : ''}
            />
            <div className="md:col-span-2 flex gap-2">
              <Button type="submit" disabled={saving}>{saving ? 'Saving...' : 'Save'}</Button>
              <Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button>
            </div>
          </form>
        )}
      </CardContent>
    </Card>
  );
};

export default ProductForm;
