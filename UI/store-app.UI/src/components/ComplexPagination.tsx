import { useLocation } from 'react-router-dom';
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from '@/components/ui/pagination';
import { constructUrl, constructPrevOrNextUrl } from '@/utils';

interface ComplexPaginationProps {
  page: number;
  totalPages: number;
}

/** First, current and last page with ellipses between, for listings that may run to many pages. */
const ComplexPaginationContainer = ({ page, totalPages: pageCount }: ComplexPaginationProps) => {
  const { search, pathname } = useLocation();

  if (pageCount < 2) return null;

  const pageButton = (pageNumber: number, isActive: boolean) => (
    <PaginationItem key={pageNumber}>
      <PaginationLink to={constructUrl({ pageNumber, search, pathname })} isActive={isActive}>
        {pageNumber}
      </PaginationLink>
    </PaginationItem>
  );

  const ellipsis = (key: string) => (
    <PaginationItem key={key}>
      <PaginationEllipsis />
    </PaginationItem>
  );

  const pages = [pageButton(1, page === 1)];
  if (page > 2) pages.push(ellipsis(`dots-before-${page}`));
  if (page !== 1 && page !== pageCount) pages.push(pageButton(page, true));
  if (page < pageCount - 1) pages.push(ellipsis(`dots-after-${page}`));
  pages.push(pageButton(pageCount, page === pageCount));

  const { prevUrl, nextUrl } = constructPrevOrNextUrl({ currentPage: page, pageCount, search, pathname });

  return (
    <Pagination className="mt-16">
      <PaginationContent>
        <PaginationItem>
          <PaginationPrevious to={prevUrl} />
        </PaginationItem>
        {pages}
        <PaginationItem>
          <PaginationNext to={nextUrl} />
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  );
};

export default ComplexPaginationContainer;
