import React, { useCallback, useEffect, useState } from 'react';
import { LanguageProvider } from './context/LanguageContext';
import { ThemeProvider } from './context/ThemeContext';
import { AuthProvider, useAuth } from './context/AuthContext';
import { Navbar } from './components/Navbar';
import { Sidebar, PageId } from './components/Sidebar';
import { ToastAlert } from './components/ToastAlert';
import { BackendConfigModal } from './components/BackendConfigModal';
import { signalRService, ConnectionStatus } from './services/signalr';
import { FlightAlert } from './types';

import { SplashScreen } from './pages/SplashScreen';
import { AuthPage } from './pages/AuthPage';
import { LiveMapPage } from './pages/LiveMapPage';
import { PriorityZonesPage } from './pages/PriorityZonesPage';
import { AlertsPage } from './pages/AlertsPage';
import { AnalyticsPage } from './pages/AnalyticsPage';
import { AdminPage } from './pages/AdminPage';

const MainAppContent: React.FC = () => {
  const { isAuthenticated, token, user, pushEnabled } = useAuth();
  const [stage, setStage] = useState<'splash' | 'app'>('splash');
  const [activePage, setActivePage] = useState<PageId>('map');
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('disconnected');
  const [unreadAlerts, setUnreadAlerts] = useState(0);
  const [alertsVersion, setAlertsVersion] = useState(0);

  const handleAlert = useCallback(
    (alert: FlightAlert) => {
      setToastMessage(alert.message);
      setUnreadAlerts((count) => count + 1);
      setAlertsVersion((version) => version + 1);

      // Native browser notification as well (only with permission / secure context)
      if (pushEnabled && 'Notification' in window && Notification.permission === 'granted') {
        try {
          new Notification('HawkEye - התראת חציית אזור', { body: alert.message, tag: alert.icao24 ?? undefined });
        } catch {
          // some browsers only allow notifications from a service worker
        }
      }
    },
    [pushEnabled]
  );

  // Real-time channel lives as long as the user is logged in
  useEffect(() => {
    if (!isAuthenticated || !token) return;
    void signalRService.startConnection(token, handleAlert, setConnectionStatus);
    return () => signalRService.stopConnection();
  }, [isAuthenticated, token, handleAlert]);

  // Non-admins cannot stay on the admin page (e.g. after a role change / logout)
  useEffect(() => {
    if (activePage === 'admin' && user?.role !== 'Admin') setActivePage('map');
  }, [activePage, user]);

  if (stage === 'splash') {
    return <SplashScreen onEnterAuth={() => setStage('app')} />;
  }

  // Unauthenticated users are strictly guarded
  if (!isAuthenticated) {
    return <AuthPage />;
  }

  const renderPage = () => {
    switch (activePage) {
      case 'map':
        return <LiveMapPage />;
      case 'zones':
        return <PriorityZonesPage />;
      case 'alerts':
        return <AlertsPage refreshKey={alertsVersion} />;
      case 'analytics':
        return <AnalyticsPage />;
      case 'admin':
        return <AdminPage />;
      default:
        return <LiveMapPage />;
    }
  };

  return (
    <div className="flex h-screen w-screen flex-col bg-[#050811] text-gray-100 overflow-hidden font-sans">
      <Navbar
        onOpenSettings={() => setIsSettingsOpen(true)}
        connectionStatus={connectionStatus}
        unreadAlerts={unreadAlerts}
        onOpenAlerts={() => {
          setUnreadAlerts(0);
          setActivePage('alerts');
        }}
      />

      <div className="flex flex-1 overflow-hidden">
        <Sidebar activePage={activePage} onSelectPage={(page) => setActivePage(page)} />
        <main className="flex-1 overflow-hidden relative">{renderPage()}</main>
      </div>

      {toastMessage && (
        <ToastAlert message={toastMessage} onClose={() => setToastMessage(null)} />
      )}

      <BackendConfigModal isOpen={isSettingsOpen} onClose={() => setIsSettingsOpen(false)} />
    </div>
  );
};

export const App: React.FC = () => {
  return (
    <LanguageProvider>
      <ThemeProvider>
        <AuthProvider>
          <MainAppContent />
        </AuthProvider>
      </ThemeProvider>
    </LanguageProvider>
  );
};

export default App;
