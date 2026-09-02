import type { Metadata } from 'next';
import { AreasPage } from '../../../views/AreasPage';

export const metadata: Metadata = {
  title: 'Áreas — SB Client',
};

export default function AreasRoute() {
  return <AreasPage />;
}