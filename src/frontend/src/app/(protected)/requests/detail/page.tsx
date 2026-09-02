import type { Metadata } from 'next';
import { Suspense } from 'react';
import { RequestDetailRoute } from '../../../../views/RequestDetailRoute';

export const metadata: Metadata = {
  title: 'Detalle de solicitud — SB Client',
};

export default function RequestDetailPageRoute() {
  return (
    <Suspense fallback={null}>
      <RequestDetailRoute />
    </Suspense>
  );
}
