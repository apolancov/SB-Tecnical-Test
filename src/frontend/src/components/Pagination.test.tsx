import { describe, expect, it } from 'vitest';
import { fireEvent, screen } from '@testing-library/react';
import { Pagination } from './Pagination';
import { renderWithProviders } from '../test/testUtils';

describe('Pagination', () => {
  it('disables Previous on the first page', () => {
    renderWithProviders(
      <Pagination page={1} totalPages={5} onChange={() => undefined} />,
    );
    expect(screen.getByRole('button', { name: /anterior/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /siguiente/i })).toBeEnabled();
  });

  it('disables Next on the last page', () => {
    renderWithProviders(
      <Pagination page={5} totalPages={5} onChange={() => undefined} />,
    );
    expect(screen.getByRole('button', { name: /anterior/i })).toBeEnabled();
    expect(screen.getByRole('button', { name: /siguiente/i })).toBeDisabled();
  });

  it('invokes onChange with the correct page on Next', () => {
    let received = 0;
    renderWithProviders(
      <Pagination page={2} totalPages={5} onChange={(next) => { received = next; }} />,
    );
    fireEvent.click(screen.getByRole('button', { name: /siguiente/i }));
    expect(received).toBe(3);
  });

  it('invokes onChange with the correct page on Previous', () => {
    let received = 0;
    renderWithProviders(
      <Pagination page={3} totalPages={5} onChange={(next) => { received = next; }} />,
    );
    fireEvent.click(screen.getByRole('button', { name: /anterior/i }));
    expect(received).toBe(2);
  });

  it('does not invoke onChange when buttons are disabled', () => {
    let called = false;
    renderWithProviders(
      <Pagination
        page={1}
        totalPages={1}
        onChange={() => { called = true; }}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /anterior/i }));
    fireEvent.click(screen.getByRole('button', { name: /siguiente/i }));
    expect(called).toBe(false);
  });
});
