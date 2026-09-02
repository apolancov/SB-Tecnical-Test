import type { Metadata } from 'next';
import { RequestsPage } from '../../../views/RequestsPage';

export const metadata: Metadata = {
  title: 'Solicitudes — SB Client',
};

export default function RequestsRoute() {
  return <RequestsPage />;
}