'use client';

import {
  useCallback,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type ChangeEvent,
  type KeyboardEvent,
} from 'react';

export interface AutocompleteSelectProps {
  readonly id?: string;
  readonly label: string;
  readonly value: string;
  readonly options?: ReadonlyArray<string>;
  readonly onChange: (next: string) => void;
  readonly disabled?: boolean;
  readonly placeholder?: string;
  readonly testId?: string;
  readonly loading?: boolean;
  readonly emptyMessage?: string;
  readonly invalid?: boolean;
}

function normalize(value: string): string {
  return value.trim().toLocaleLowerCase();
}

function filterOptions(
  options: ReadonlyArray<string>,
  query: string,
): ReadonlyArray<string> {
  const needle = normalize(query);
  if (needle.length === 0) {
    return options;
  }
  return options.filter((option) => normalize(option).includes(needle));
}

export function AutocompleteSelect({
  id,
  label,
  value,
  options,
  onChange,
  disabled = false,
  placeholder,
  testId,
  loading = false,
  emptyMessage = 'Sin coincidencias.',
  invalid = false,
}: AutocompleteSelectProps) {
  const generatedId = useId();
  const fieldId = id ?? generatedId;
  const listboxId = `${fieldId}-listbox`;

  const [isOpen, setIsOpen] = useState<boolean>(false);
  const [activeIndex, setActiveIndex] = useState<number>(-1);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);

  const filteredOptions = useMemo<ReadonlyArray<string>>(
    () => filterOptions(options ?? [], value),
    [options, value],
  );

  const closeList = useCallback(() => {
    setIsOpen(false);
    setActiveIndex(-1);
  }, []);

  useEffect(() => {
    if (!isOpen) {
      return;
    }
    const handleClickOutside = (event: MouseEvent) => {
      const container = containerRef.current;
      if (container === null) {
        return;
      }
      if (!container.contains(event.target as Node)) {
        closeList();
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen, closeList]);

  useEffect(() => {
    if (activeIndex >= filteredOptions.length) {
      setActiveIndex(filteredOptions.length - 1);
    }
  }, [activeIndex, filteredOptions.length]);

  const handleInputChange = (event: ChangeEvent<HTMLInputElement>) => {
    onChange(event.target.value);
    setIsOpen(true);
    setActiveIndex(-1);
  };

  const handleFocus = () => {
    if (disabled) {
      return;
    }
    setIsOpen(true);
  };

  const selectOption = (option: string) => {
    onChange(option);
    closeList();
    inputRef.current?.focus();
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (!isOpen) {
        setIsOpen(true);
        setActiveIndex(0);
        return;
      }
      setActiveIndex((current) => {
        if (filteredOptions.length === 0) {
          return -1;
        }
        return (current + 1) % filteredOptions.length;
      });
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (!isOpen) {
        setIsOpen(true);
        setActiveIndex(filteredOptions.length - 1);
        return;
      }
      setActiveIndex((current) => {
        if (filteredOptions.length === 0) {
          return -1;
        }
        return current <= 0 ? filteredOptions.length - 1 : current - 1;
      });
      return;
    }

    if (event.key === 'Enter') {
      if (isOpen && activeIndex >= 0 && activeIndex < filteredOptions.length) {
        event.preventDefault();
        selectOption(filteredOptions[activeIndex]);
      }
      return;
    }

    if (event.key === 'Escape') {
      if (isOpen) {
        event.preventDefault();
        closeList();
      }
      return;
    }

    if (event.key === 'Tab') {
      closeList();
    }
  };

  const showListbox = isOpen && !disabled;
  const hasOptions = filteredOptions.length > 0;

  return (
    <div className="field autocomplete" ref={containerRef}>
      <label className="field__label" htmlFor={fieldId}>
        {label}
      </label>
      <input
        ref={inputRef}
        id={fieldId}
        type="text"
        role="combobox"
        aria-autocomplete="list"
        aria-expanded={showListbox}
        aria-controls={listboxId}
        aria-activedescendant={
          showListbox && activeIndex >= 0 && activeIndex < filteredOptions.length
            ? `${listboxId}-option-${activeIndex}`
            : undefined
        }
        aria-busy={loading}
        aria-invalid={invalid ? 'true' : 'false'}
        autoComplete="off"
        spellCheck={false}
        value={value}
        placeholder={placeholder}
        disabled={disabled}
        data-testid={testId}
        onChange={handleInputChange}
        onFocus={handleFocus}
        onKeyDown={handleKeyDown}
      />
      {showListbox && hasOptions && (
        <ul
          id={listboxId}
          role="listbox"
          className="autocomplete__listbox"
          data-testid={testId !== undefined ? `${testId}-listbox` : undefined}
        >
          {filteredOptions.map((option, index) => {
            const isActive = index === activeIndex;
            return (
              <li
                key={`${fieldId}-option-${index}`}
                id={`${listboxId}-option-${index}`}
                role="option"
                aria-selected={isActive}
                className={
                  isActive
                    ? 'autocomplete__option autocomplete__option--active'
                    : 'autocomplete__option'
                }
                onMouseDown={(event) => {
                  event.preventDefault();
                  selectOption(option);
                }}
                onMouseEnter={() => setActiveIndex(index)}
              >
                {option}
              </li>
            );
          })}
        </ul>
      )}
      {showListbox && !hasOptions && !loading && (
        <div
          className="autocomplete__empty"
          data-testid={testId !== undefined ? `${testId}-empty` : undefined}
        >
          {emptyMessage}
        </div>
      )}
      {loading && (
        <span className="autocomplete__status" role="status">
          Cargando opciones...
        </span>
      )}
    </div>
  );
}
