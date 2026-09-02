import type { Metadata } from 'next';
import { RequestTypesPage } from '../../../views/RequestTypesPage';

export const metadata: Metadata = {
  title: 'Tipos de solicitud — SB Client',
};

export default function RequestTypesRoute() {
  return <RequestTypesPage />;
}