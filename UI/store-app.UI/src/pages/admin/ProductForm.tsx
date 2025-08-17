import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { customFetch } from '@/utils';
import type { ProductData } from '@/utils/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import FormInput from '@/components/FormInput';
import FormCheckbox from '@/components/FormCheckbox';

const ProductForm = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const editing = !!id && id !== 'new';
  const [product, setProduct] = useState<ProductData | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (editing) {
      (async () => {
        const res = await customFetch.get(`/products/${id}`);
        setProduct(res.data.data);
      })();
    }
  }, [editing, id]);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    const form = e.target as HTMLFormElement;
    const fd = new FormData(form);
    const payload = Object.fromEntries(fd.entries());
    try {
      if (editing) {
        await customFetch.put(`/products/${id}`, payload);
      } else {
        await customFetch.post(`/products`, payload);
      }
      navigate('/admin/products');
    } finally {
      setSaving(false);
    }
  };

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
          {(() => {
      const attrs = product?.attributes;
      const sale = attrs?.salePrice ?? '';
            return (
              <>
                <FormInput type="text" name="title" label="title" defaultValue={attrs?.title} />
                <FormInput type="text" name="company" label="company" defaultValue={attrs?.company} />
                <FormInput type="text" name="category" label="category" defaultValue={attrs?.category} />
                <FormInput type="number" name="price" label="price" defaultValue={attrs?.price} />
                <FormInput type="url" name="image" label="image url" defaultValue={attrs?.image} />
                <FormInput type="number" name="salePrice" label="sale price" defaultValue={sale} />
                <FormCheckbox name="featured" label="featured" defaultValue={attrs?.featured ? 'on' : undefined} />
                <div className="md:col-span-2">
                  <FormInput type="text" name="description" label="description (max 4000)" defaultValue={attrs?.description} />
                </div>
        <FormInput type="number" step="0.01" name="widthCm" label="width (cm)" defaultValue={attrs?.widthCm ?? ''} />
        <FormInput type="number" step="0.01" name="heightCm" label="height (cm)" defaultValue={attrs?.heightCm ?? ''} />
        <FormInput type="number" step="0.01" name="depthCm" label="depth (cm)" defaultValue={attrs?.depthCm ?? ''} />
        <FormInput type="number" step="0.01" name="weightKg" label="weight (kg)" defaultValue={attrs?.weightKg ?? ''} />
        <FormInput type="text" name="materials" label="materials" defaultValue={attrs?.materials ?? ''} />
                <div className="md:col-span-2 flex gap-2">
                  <Button type="submit" disabled={saving}>{saving ? 'Saving...' : 'Save'}</Button>
                  <Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button>
                </div>
              </>
            );
          })()}
        </form>
        )}
      </CardContent>
    </Card>
  );
};

export default ProductForm;
