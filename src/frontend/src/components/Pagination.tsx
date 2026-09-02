'use client';

export interface PaginationProps {
  readonly page: number;
  readonly totalPages: number;
  readonly onChange: (nextPage: number) => void;
}

const MinimumVisiblePages = 1;

export function Pagination({ page, totalPages, onChange }: PaginationProps) {
  const safeTotalPages = Math.max(totalPages, MinimumVisiblePages);
  const isFirstPage = page <= 1;
  const isLastPage = page >= safeTotalPages;

  const handlePrevious = () => {
    if (!isFirstPage) {
      onChange(page - 1);
    }
  };

  const handleNext = () => {
    if (!isLastPage) {
      onChange(page + 1);
    }
  };

  return (
    <nav aria-label="Paginación" className="pagination">
      <button
        type="button"
        onClick={handlePrevious}
        disabled={isFirstPage}
        aria-label="Página anterior"
        className="button"
      >
        Anterior
      </button>
      <span aria-live="polite" className="pagination__label">
        Página {page} de {safeTotalPages}
      </span>
      <button
        type="button"
        onClick={handleNext}
        disabled={isLastPage}
        aria-label="Página siguiente"
        className="button"
      >
        Siguiente
      </button>
    </nav>
  );
}
