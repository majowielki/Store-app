import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  useCreateProductMutation,
  useGetProductForAdminQuery,
  useGetProductsMetaQuery,
  useSetProductStockMutation,
  useUpdateProductMutation,
} from '@/api/catalog';
import type { ProductCategory, ProductCompany, ProductDetail, ProductHotspot, ProductImage, ProductPayload, ProductsMeta } from '@/api/types';
import FormCheckbox from '@/components/FormCheckbox';
import FormInput from '@/components/FormInput';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useFinishes } from '@/hooks/use-finishes';
import { toast } from '@/hooks/use-toast';
import HotspotEditor from './content/HotspotEditor';
import FinishPicker from './FinishPicker';
import GalleryEditor from './GalleryEditor';

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
  stockQuantity: optionalNumber(fd.get('stockQuantity')) ?? 0,
});

const ProductForm = () => {
  const { id } = useParams<{ id: string }>();
  const editing = id !== undefined && id !== 'new';
  // Always a fresh read, inactive products too: the form is filled once, from what it returns
  const { data: product, isFetching } = useGetProductForAdminQuery(Number(id), { skip: !editing, refetchOnMountOrArgChange: true });
  // Meta is only used for hints; the form still works without it
  const { data: meta } = useGetProductsMetaQuery();

  return (
    <Card>
      <CardHeader>
        <CardTitle>{editing ? 'Edit' : 'Add'} Product</CardTitle>
        {editing && product && (
          <p className="text-sm text-muted-foreground">
            Slug <code className="font-mono">{product.slug}</code> - made from the first title, kept when the title changes.
          </p>
        )}
      </CardHeader>
      <CardContent>
        {editing && (isFetching || !product) ? (
          <div className="text-sm text-muted-foreground">{isFetching ? 'Loading product…' : 'Product not found.'}</div>
        ) : (
          <ProductFields key={product?.id ?? 'new'} id={editing ? Number(id) : null} product={editing ? product : undefined} meta={meta} />
        )}
      </CardContent>
    </Card>
  );
};

interface ProductFieldsProps {
  id: number | null;
  product?: ProductDetail;
  meta?: ProductsMeta;
}

/** The form itself; the gallery and the points are kept in state, the other fields in the form. */
const ProductFields = ({ id, product, meta }: ProductFieldsProps) => {
  const navigate = useNavigate();
  const editing = id !== null;
  const [mainImage, setMainImage] = useState(product?.image ?? '');
  const [images, setImages] = useState<ProductImage[]>(product?.images ?? []);
  const [hotspots, setHotspots] = useState<ProductHotspot[]>(product?.hotspots ?? []);
  // Typed or picked from the swatches; the form sends what the field holds
  const [colors, setColors] = useState(product?.colors.join(', ') ?? '');
  const { finishes } = useFinishes();
  const toggleFinish = (key: string) =>
    setColors((text) => {
      const keys = splitList(text);
      return (keys.includes(key) ? keys.filter((k) => k !== key) : [...keys, key]).join(', ');
    });
  const [createProduct, { isLoading: creating }] = useCreateProductMutation();
  const [updateProduct, { isLoading: updating }] = useUpdateProductMutation();
  const [setProductStock, { isLoading: stocking }] = useSetProductStockMutation();
  const saving = creating || updating || stocking;

  // A refusal (validation 422 with the field messages, or 403 for the demo administrator)
  // has been reported by the error middleware
  const onSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const pointsWithoutProduct = hotspots.filter((point) => !point.productSlug).length;
    if (pointsWithoutProduct > 0) {
      toast({ variant: 'destructive', description: `Choose a product for every point (${pointsWithoutProduct} without one).` });
      return;
    }
    const payload = { ...toPayload(new FormData(e.currentTarget)), images, hotspots };
    try {
      if (editing) {
        // The stock has its own endpoint: it is counted in the warehouse, not edited with the product
        const { stockQuantity, ...fields } = payload;
        await updateProduct({ id, body: { ...fields, isActive: true } }).unwrap();
        if (stockQuantity !== product?.stockQuantity) {
          await setProductStock({ id, stockQuantity }).unwrap();
        }
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
    <form onSubmit={onSubmit} className="grid gap-4 md:grid-cols-2">
      <FormInput type="text" name="title" label="title" defaultValue={product?.title} required minLength={3} maxLength={200} />
      <FormInput type="text" name="company" label="company" defaultValue={product?.company} required placeholder={hint(meta?.companies)} />
      <FormInput type="text" name="category" label="category" defaultValue={product?.category} required placeholder={hint(meta?.categories)} />
      <FormInput type="number" name="price" label="price" defaultValue={product?.price} required min={0.01} step="0.01" />
      <FormInput type="url" name="image" label="image url" defaultValue={product?.image} required onChange={(e) => setMainImage(e.target.value.trim())} />
      <FormInput type="number" name="salePrice" label="sale price" defaultValue={product?.salePrice ?? ''} min={0.01} step="0.01" />
      <div className="grid gap-3 md:col-span-2">
        <FormInput
          type="text"
          name="colors"
          label="colors (comma separated)"
          value={colors}
          onChange={(e) => setColors(e.target.value)}
          required
          placeholder={hint(finishes.map((finish) => finish.key))}
        />
        <FinishPicker finishes={finishes} chosen={splitList(colors)} onToggle={toggleFinish} />
      </div>
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
      <div className="grid gap-2">
        <FormInput type="number" name="stockQuantity" label="units on hand" defaultValue={product?.stockQuantity ?? 0} required min={product?.reservedQuantity ?? 0} max={100000} step="1" />
        {editing && product && (
          <p className="text-xs text-muted-foreground">
            {product.reservedQuantity ?? 0} held for orders not shipped yet, {product.availableQuantity} available.
          </p>
        )}
      </div>
      <div className="grid gap-8 border-t pt-6 md:col-span-2">
        <GalleryEditor images={images} onChange={setImages} />
        <HotspotEditor image={mainImage} hotspots={hotspots} onChange={setHotspots} />
      </div>
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
  );
};

export default ProductForm;
