import type { Metadata } from 'next';
import { InstitutionsPage } from '../../../views/InstitutionsPage';

export const metadata: Metadata = {
  title: 'Instituciones — SB Client',
};

export default function InstitutionsRoute() {
  return <InstitutionsPage />;
}
