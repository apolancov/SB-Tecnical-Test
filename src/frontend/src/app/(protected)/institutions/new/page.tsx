import type { Metadata } from 'next';
import { NewInstitutionPage } from '../../../../views/NewInstitutionPage';

export const metadata: Metadata = {
  title: 'Nueva institución — SB Client',
};

export default function NewInstitutionRoute() {
  return <NewInstitutionPage />;
}
