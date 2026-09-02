'use client';

import { useService } from '../hooks/ServiceContext';
import { useDashboard } from '../hooks/useDashboard';
import { DashboardSummaryView } from '../components/DashboardSummaryView';

export function DashboardPage() {
  const { dashboardService } = useService();
  const { summary, loading, error, refresh } = useDashboard({
    service: dashboardService,
  });

  return (
    <section className="page" data-testid="dashboard-page">
      <header className="page__header">
        <div>
          <h1 className="page__title">Dashboard</h1>
          <p className="page__subtitle">
            Resumen operativo de las solicitudes registradas.
          </p>
        </div>
      </header>
      <DashboardSummaryView
        summary={summary}
        loading={loading}
        error={error}
        onRetry={refresh}
      />
    </section>
  );
}