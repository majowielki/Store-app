import { useLocation } from 'react-router-dom';
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from '@/components/ui/pagination';
import { constructUrl, constructPrevOrNextUrl } from '@/utils';

interface PaginationContainerProps {
  page: number;
  totalPages: number;
}

/** Every page as a link, for listings with a handful of pages. */
const PaginationContainer = ({ page, totalPages: pageCount }: PaginationContainerProps) => {
  const { search, pathname } = useLocation();

  if (pageCount < 2) return null;

  const pages = Array.from({ length: pageCount }, (_, index) => index + 1);
  const { prevUrl, nextUrl } = constructPrevOrNextUrl({ currentPage: page, pageCount, search, pathname });

  return (
    <Pagination className="mt-16">
      <PaginationContent>
        <PaginationItem>
          <PaginationPrevious to={prevUrl} />
        </PaginationItem>
        {pages.map((pageNumber) => (
          <PaginationItem key={pageNumber}>
            <PaginationLink to={constructUrl({ pageNumber, search, pathname })} isActive={pageNumber === page}>
              {pageNumber}
            </PaginationLink>
          </PaginationItem>
        ))}
        <PaginationItem>
          <PaginationNext to={nextUrl} />
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  );
};
export default PaginationContainer;
