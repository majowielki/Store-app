import { Pagination, PaginationContent, PaginationItem, PaginationLink } from '@/components/ui/pagination';

interface PageNumbersProps {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

/** Page buttons for the admin tables, which keep their page in state rather than in the URL. */
const PageNumbers = ({ page, totalPages, onPageChange }: PageNumbersProps) => {
  if (totalPages < 2) return null;
  return (
    <div className="px-2 py-3">
      <Pagination>
        <PaginationContent>
          {Array.from({ length: totalPages }, (_, i) => (
            <PaginationItem key={i}>
              <PaginationLink
                to="#"
                isActive={page === i + 1}
                onClick={(e) => {
                  e.preventDefault();
                  onPageChange(i + 1);
                }}
              >
                {i + 1}
              </PaginationLink>
            </PaginationItem>
          ))}
        </PaginationContent>
      </Pagination>
    </div>
  );
};

export default PageNumbers;
