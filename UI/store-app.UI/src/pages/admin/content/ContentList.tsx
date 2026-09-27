import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ExternalLink, Trash2 } from 'lucide-react';
import { useDeleteContentMutation, useGetContentAdminQuery, type ContentEntry, type ContentKind } from '@/api/content';
import ConfirmDialog from '@/components/ConfirmDialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { toast } from '@/hooks/use-toast';
import { formatDateTime } from '@/utils';
import { contentKinds, entryTitle, isContentKind } from './contentKinds';

/** Every entry of one kind of content, published or not. */
const ContentList = () => {
  const { kind } = useParams<{ kind: string }>();
  if (!isContentKind(kind)) return <p className="p-6">Unknown kind of content.</p>;
  return <EntryList key={kind} kind={kind} />;
};

const EntryList = ({ kind }: { kind: ContentKind }) => {
  const config = contentKinds[kind];
  const { data: entries, isLoading } = useGetContentAdminQuery(kind);
  const [deleteContent, { isLoading: deleting }] = useDeleteContentMutation();
  const [toDelete, setToDelete] = useState<ContentEntry<ContentKind> | null>(null);

  // A refusal (the demo administrator, 403) has been reported by the error middleware
  const confirmDelete = async () => {
    if (!toDelete) return;
    try {
      await deleteContent({ kind, id: toDelete.id }).unwrap();
      toast({ description: `${entryTitle(toDelete)} deleted.` });
    } catch {
      // reported
    } finally {
      setToDelete(null);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">{config.label}</h2>
        <Button asChild>
          <Link to={`/admin/content/${kind}/new`}>Add a {config.singular}</Link>
        </Button>
      </div>
      <Card className="p-2">
        {isLoading ? (
          <div className="p-6">Loading...</div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Title</TableHead>
                <TableHead>Address</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Changed</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {entries?.map((entry) => (
                <TableRow key={entry.id}>
                  <TableCell className="font-medium">{entryTitle(entry)}</TableCell>
                  <TableCell className="font-mono text-xs text-muted-foreground">{entry.slug}</TableCell>
                  <TableCell>{entry.isPublished ? <Badge>Published</Badge> : <Badge variant="outline">Draft</Badge>}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{formatDateTime(entry.updatedAt)}</TableCell>
                  <TableCell>
                    <div className="flex items-center justify-end gap-2">
                      {entry.isPublished && (
                        <Button asChild size="sm" variant="ghost" aria-label={`Open ${entryTitle(entry)} in the shop`}>
                          <Link to={config.publicPath(entry.slug)} target="_blank">
                            <ExternalLink />
                          </Link>
                        </Button>
                      )}
                      <Button asChild size="sm" variant="outline">
                        <Link to={`/admin/content/${kind}/${entry.id}`}>Edit</Link>
                      </Button>
                      <Button size="sm" variant="destructive" aria-label={`Delete ${entryTitle(entry)}`} onClick={() => setToDelete(entry)}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>
      <ConfirmDialog
        open={toDelete !== null}
        title={`Delete this ${config.singular}?`}
        description={`"${toDelete ? entryTitle(toDelete) : ''}" disappears from the shop and from this list. To only hide it, unpublish it instead.`}
        confirmLabel="Delete"
        busy={deleting}
        onConfirm={confirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </div>
  );
};

export default ContentList;
