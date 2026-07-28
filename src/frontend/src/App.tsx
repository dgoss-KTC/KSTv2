import { useBackendStatus } from './hooks/useBackendStatus';
import { StatusDashboard } from './components/StatusDashboard';
import './App.css';

function App() {
  const { connectionState, status, errorMessage, lastUpdated, refresh } =
    useBackendStatus();

  return (
    <main>
      <StatusDashboard
        connectionState={connectionState}
        status={status}
        errorMessage={errorMessage}
        lastUpdated={lastUpdated}
        onRetry={refresh}
        onRefresh={refresh}
      />
    </main>
  );
}

export default App;
