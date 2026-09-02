import type { Metadata } from 'next';
import { NewRequestPage } from '../../../../views/NewRequestPage';

export const metadata: Metadata = {
  title: 'Nueva solicitud — SB Client',
};

export default function NewRequestRoute() {
  return <NewRequestPage />;
}