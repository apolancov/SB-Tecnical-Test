import type { Metadata, Viewport } from 'next';
import type { ReactNode } from 'react';
import { ClientProviders } from './ClientProviders';
import './globals.css';

export const metadata: Metadata = {
  title: 'SB Client',
  description: 'SB Client — Consola administrativa de instituciones',
};

export const viewport: Viewport = {
  width: 'device-width',
  initialScale: 1,
};

export default function RootLayout({ children }: { readonly children: ReactNode }) {
  return (
    <html lang="es">
      <body>
        <ClientProviders>{children}</ClientProviders>
      </body>
    </html>
  );
}
