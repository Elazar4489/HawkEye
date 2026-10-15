import React from 'react';
import { Map, ShieldAlert, Bell, BarChart3, UserCog } from 'lucide-react';
import { useLanguage } from '../context/LanguageContext';
import { useAuth } from '../context/AuthContext';

export type PageId = 'map' | 'zones' | 'alerts' | 'analytics' | 'admin';

interface SidebarProps {
  activePage: PageId;
  onSelectPage: (page: PageId) => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ activePage, onSelectPage }) => {
  const { t } = useLanguage();
  const { user } = useAuth();

  const navItems = [
    { id: 'map', label: t('navMap'), icon: Map, color: 'text-cyan-400' },
    { id: 'zones', label: t('navZones'), icon: ShieldAlert, color: 'text-emerald-400' },
    { id: 'alerts', label: t('navAlerts'), icon: Bell, color: 'text-rose-400', badge: 'LIVE' },
    { id: 'analytics', label: t('navAnalytics'), icon: BarChart3, color: 'text-purple-400' },
    ...(user?.role === 'Admin'
      ? [{ id: 'admin', label: t('navAdmin'), icon: UserCog, color: 'text-amber-400' }]
      : [])
  ];

  return (
    <aside className="w-64 flex-shrink-0 border-l border-cyan-500/20 bg-[#050811]/95 p-4 backdrop-blur-xl hidden md:flex flex-col justify-between">
      <div className="space-y-2">
        <div className="px-3 py-2 text-[11px] font-mono uppercase tracking-wider text-cyan-400/60 font-semibold">
          ממשק ניווט מרכזי
        </div>

        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = activePage === item.id;
          return (
            <button
              key={item.id}
              onClick={() => onSelectPage(item.id as PageId)}
              className={`flex w-full items-center justify-between rounded-xl px-4 py-3 text-sm font-semibold transition-all duration-200 border ${
                isActive
                  ? 'border-cyan-500/50 bg-gradient-to-r from-cyan-500/20 to-blue-600/10 text-white shadow-lg shadow-cyan-500/10'
                  : 'border-transparent text-gray-400 hover:border-gray-800 hover:bg-[#0B0F19] hover:text-white'
              }`}
            >
              <div className="flex items-center space-x-3 space-x-reverse">
                <Icon size={18} className={isActive ? item.color : 'text-gray-500'} />
                <span>{item.label}</span>
              </div>
              {item.badge && (
                <span className="rounded bg-rose-500/20 px-1.5 py-0.5 text-[10px] font-bold text-rose-400 border border-rose-500/30 animate-pulse">
                  {item.badge}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* Cyber Footer Card */}
      <div className="rounded-xl border border-cyan-500/20 bg-[#0B0F19]/80 p-3 text-xs text-gray-400 space-y-1">
        <div className="font-mono text-[10px] text-cyan-400">ENGINE STATUS</div>
        <div className="flex justify-between items-center">
          <span>Spatial Matcher:</span>
          <span className="font-mono text-emerald-400 font-bold">0.8ms</span>
        </div>
        <div className="flex justify-between items-center">
          <span>Broker Latency:</span>
          <span className="font-mono text-cyan-400 font-bold">12ms</span>
        </div>
      </div>
    </aside>
  );
};
