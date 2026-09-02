import { useState } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AutocompleteSelect } from './AutocompleteSelect';

describe('AutocompleteSelect', () => {
  const originalGetByRole = screen.getByRole;

  beforeEach(() => {
    void originalGetByRole;
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders an input with the provided label', () => {
    render(
      <AutocompleteSelect
        id="test-combo"
        label="My label"
        value=""
        options={['Apple', 'Banana']}
        onChange={() => undefined}
      />,
    );

    expect(screen.getByLabelText('My label')).toBeInTheDocument();
  });

  it('opens the listbox on focus and shows matching options', async () => {
    const user = userEvent.setup();
    render(
      <AutocompleteSelect
        id="test-combo"
        label="My label"
        value=""
        options={['Apple', 'Banana', 'Cherry']}
        onChange={() => undefined}
      />,
    );

    const input = screen.getByLabelText('My label');
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();

    await user.click(input);

    expect(screen.getByRole('listbox')).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Apple' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Banana' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Cherry' })).toBeInTheDocument();
  });

  it('filters options as the user types', async () => {
    const ControlledWrapper = () => {
      const [value, setValue] = useState<string>('');
      return (
        <AutocompleteSelect
          id="test-combo"
          label="My label"
          value={value}
          options={['Apple', 'Banana', 'Cherry']}
          onChange={setValue}
        />
      );
    };

    const user = userEvent.setup();
    render(<ControlledWrapper />);

    const input = screen.getByLabelText('My label');
    await user.click(input);
    await user.keyboard('an');

    expect(input).toHaveValue('an');
    expect(screen.queryByRole('option', { name: 'Apple' })).not.toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Banana' })).toBeInTheDocument();
  });

  it('emits the selected option and closes the listbox', async () => {
    const ControlledWrapper = () => {
      const [value, setValue] = useState<string>('');
      return (
        <AutocompleteSelect
          id="test-combo"
          label="My label"
          value={value}
          options={['Apple', 'Banana']}
          onChange={setValue}
        />
      );
    };

    const user = userEvent.setup();
    render(<ControlledWrapper />);

    const input = screen.getByLabelText('My label');
    await user.click(input);
    await user.click(screen.getByRole('option', { name: 'Banana' }));

    expect(input).toHaveValue('Banana');
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('navigates options with ArrowDown / ArrowUp and selects with Enter', async () => {
    const ControlledWrapper = () => {
      const [value, setValue] = useState<string>('');
      return (
        <AutocompleteSelect
          id="test-combo"
          label="My label"
          value={value}
          options={['Apple', 'Banana', 'Cherry']}
          onChange={setValue}
        />
      );
    };

    const user = userEvent.setup();
    render(<ControlledWrapper />);

    const input = screen.getByLabelText('My label');
    await user.click(input);
    await user.keyboard('{ArrowDown}{ArrowDown}{ArrowDown}{ArrowUp}');
    await user.keyboard('{Enter}');

    expect(input).toHaveValue('Banana');
  });

  it('closes the listbox on Escape', async () => {
    const user = userEvent.setup();
    render(
      <AutocompleteSelect
        id="test-combo"
        label="My label"
        value=""
        options={['Apple', 'Banana']}
        onChange={() => undefined}
      />,
    );

    await user.click(screen.getByLabelText('My label'));
    expect(screen.getByRole('listbox')).toBeInTheDocument();
    await user.keyboard('{Escape}');
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('shows an empty message when no options match', async () => {
    const ControlledWrapper = () => {
      const [value, setValue] = useState<string>('');
      return (
        <AutocompleteSelect
          id="test-combo"
          label="My label"
          value={value}
          options={['Apple']}
          onChange={setValue}
          emptyMessage="No hay coincidencias"
        />
      );
    };

    const user = userEvent.setup();
    render(<ControlledWrapper />);

    const input = screen.getByLabelText('My label');
    await user.click(input);
    await user.keyboard('zzz');

    expect(input).toHaveValue('zzz');
    expect(screen.getByText('No hay coincidencias')).toBeInTheDocument();
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('keeps a free-text value that is not in the option list', async () => {
    const ControlledWrapper = () => {
      const [value, setValue] = useState<string>('');
      return (
        <AutocompleteSelect
          id="test-combo"
          label="My label"
          value={value}
          options={['Apple']}
          onChange={setValue}
        />
      );
    };

    const user = userEvent.setup();
    render(<ControlledWrapper />);

    const input = screen.getByLabelText('My label');
    await user.click(input);
    await user.keyboard('Custom value');

    expect(input).toHaveValue('Custom value');
  });

  it('does not open the listbox when disabled', async () => {
    const user = userEvent.setup();
    render(
      <AutocompleteSelect
        id="test-combo"
        label="My label"
        value=""
        options={['Apple']}
        onChange={() => undefined}
        disabled={true}
      />,
    );

    await user.click(screen.getByLabelText('My label'));
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });
});
