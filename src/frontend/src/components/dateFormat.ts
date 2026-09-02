export interface FormatDateTimeOptions {
  readonly locale?: string;
  readonly fallback?: string;
}

const DefaultLocale = 'es-DO';

const DateTimeFormatterCache = new Map<string, Intl.DateTimeFormat>();

function getDateTimeFormatter(locale: string): Intl.DateTimeFormat {
  let formatter = DateTimeFormatterCache.get(locale);
  if (formatter === undefined) {
    formatter = new Intl.DateTimeFormat(locale, {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
    });
    DateTimeFormatterCache.set(locale, formatter);
  }
  return formatter;
}

export function formatDateTime(
  value: string | null | undefined,
  options: FormatDateTimeOptions = {},
): string {
  if (value === null || value === undefined || value.length === 0) {
    return options.fallback ?? '—';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return options.fallback ?? value;
  }

  const locale = options.locale ?? DefaultLocale;
  return getDateTimeFormatter(locale).format(date);
}

export function formatDate(value: string | null | undefined, options: FormatDateTimeOptions = {}): string {
  if (value === null || value === undefined || value.length === 0) {
    return options.fallback ?? '—';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return options.fallback ?? value;
  }

  return new Intl.DateTimeFormat(options.locale ?? DefaultLocale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date);
}