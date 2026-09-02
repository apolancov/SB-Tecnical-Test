'use client';

import { useSearchParams } from 'next/navigation';
import { RequestDetailPage } from './RequestDetailPage';

export function RequestDetailRoute() {
  const searchParams = useSearchParams();
  const id = searchParams?.get('id');
  const requestId = id === null || id === undefined || id.length === 0 ? null : id;

  return <RequestDetailPage requestId={requestId} />;
}