import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
  PaginationEllipsis,
} from '@/components/ui/pagination';
import {
  type OrdersResponse,
  constructUrl,
  constructPrevOrNextUrl,
} from '@/utils';
import { useLoaderData, useLocation } from 'react-router-dom';

const ComplexPaginationContainer = () => {
  const { meta } = useLoaderData() as OrdersResponse;
  const { pageCount, page } = meta.pagination;
  const { search, pathname } = useLocation();

  if (pageCount < 2) return null;

  const constructButton = ({
    pageNumber,
    isActive,
  }: {
    pageNumber: number;
    isActive: boolean;
  }): React.ReactNode => {
    const url = constructUrl({ pageNumber, search, pathname });
    return (
      <PaginationItem key={pageNumber}>
        <PaginationLink to={url} isActive={isActive}>
          {pageNumber}
        </PaginationLink>
      </PaginationItem>
    );
  };

  const constructEllipsis = (key: string): React.ReactNode => (
    <PaginationItem key={key}>
      <PaginationEllipsis />
    </PaginationItem>
  );

  const renderPagination = () => {
    const pages: React.ReactNode[] = [];
    // first page
    pages.push(constructButton({ pageNumber: 1, isActive: page === 1 }));

    // ellipsis before current page
    if (page > 2) {
      pages.push(constructEllipsis('dots-1'));
    }

    // active page (if not first or last)
    if (page !== 1 && page !== pageCount) {
      pages.push(constructButton({ pageNumber: page, isActive: true }));
    }

    // ellipsis after current page
    if (page < pageCount - 1) {
      pages.push(constructEllipsis('dots-2'));
    }

    // last page
    pages.push(
      constructButton({ pageNumber: pageCount, isActive: page === pageCount })
    );
    return pages;
  };

  const { prevUrl, nextUrl } = constructPrevOrNextUrl({
    currentPage: page,
    pageCount,
    search,
    pathname,
  });

  return (
    <Pagination className="mt-16">
      <PaginationContent>
        <PaginationItem>
          <PaginationPrevious to={prevUrl} />
        </PaginationItem>
        {renderPagination()}
        <PaginationItem>
          <PaginationNext to={nextUrl} />
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  );
};

export default ComplexPaginationContainer;
  