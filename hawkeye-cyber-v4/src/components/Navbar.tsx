import React from 'react';
import { Eye, Bell, Globe, Sun, Moon, Shield, Settings, User as UserIcon, LogOut } from 'lucide-react';
import { useLanguage } from '../context/LanguageContext';
import { useTheme } from '../context/ThemeContext';
import { useAuth } from '../context/AuthContext';
import { ConnectionStatus } from '../services/signalr';

interface NavbarProps {
  onOpenSettings: () => void;
  connectionStatus: ConnectionStatus;
  unreadAlerts: number;
  onOpenAlerts: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onOpenSettings, connectionStatus, unreadAlerts, onOpenAlerts }) => {
  const isLive = connectionStatus === 'connected';
  const { t, lang, toggleLanguage } = useLanguage();
  const { mapTheme, toggleMapTheme } = useTheme();
  const { user, logout } = useAuth();

  return (
    <header className="sticky top-0 z-40 flex h-16 w-full items-center justify-between border-b border-cyan-500/20 bg-[#050811]/90 px-6 backdrop-blur-xl">
      {/* Brand & Status Indicator */}
      <div className="flex items-center space-x-4 space-x-reverse">
        <div className="flex items-center space-x-2 space-x-reverse group cursor-pointer">
          <div className="relative flex items-center justify-center rounded-xl bg-cyan-500/10 p-2 text-cyan-400 border border-cyan-500/30 group-hover:border-cyan-400 group-hover:shadow-lg group-hover:shadow-cyan-500/20 transition">
            <Eye size={24} className="animate-pulse" />
            <div className="absolute -top-1 -right-1 h-2.5 w-2.5 rounded-full bg-cyan-400 animate-ping" />
          </div>
          <div>
            <span className="text-xl font-extrabold tracking-wider bg-gradient-to-r from-cyan-400 via-blue-400 to-indigo-500 bg-clip-text text-transparent font-mono">
              HAWKEYE
            </span>
            <span className="mr-2 text-[10px] font-bold text-cyan-400/80 bg-cyan-500/10 px-1.5 py-0.5 rounded border border-cyan-500/20 uppercase tracking-widest">
              v4.0 Cyber
            </span>
          </div>
        </div>

        {/* Live SignalR Connection Badge */}
        <div
          className={`hidden md:flex items-center space-x-2 space-x-reverse rounded-full px-3 py-1 border text-xs font-semibold ${
            isLive
              ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
              : 'bg-amber-500/10 border-amber-500/30 text-amber-400'
          }`}
        >
          <span className={`h-2 w-2 rounded-full animate-pulse ${isLive ? 'bg-emerald-400' : 'bg-amber-400'}`} />
          <span>{t(isLive ? 'liveConnected' : 'liveDisconnected')}</span>
        </div>

        {/* HTTPS Badge */}
        <div className="hidden lg:flex items-center space-x-1.5 space-x-reverse rounded-full bg-blue-500/10 px-2.5 py-1 border border-blue-500/30 text-blue-400 text-[11px] font-mono">
          <Shield size={12} />
          <span>{window.location.protocol.replace(':', '').toUpperCase()} {window.location.host}</span>
        </div>
      </div>

      {/* Right Controls */}
      <div className="flex items-center space-x-3 space-x-reverse">
        {/* Map Theme Toggle Button */}
        <button
          onClick={toggleMapTheme}
          title={t(mapTheme === 'dark' ? 'mapLightTheme' : 'mapDarkTheme')}
          className="flex items-center space-x-1.5 space-x-reverse rounded-xl border border-cyan-500/30 bg-cyan-500/10 px-3 py-1.5 text-xs font-semibold text-cyan-300 hover:bg-cyan-500/20 transition shadow-sm"
        >
          {mapTheme === 'dark' ? <Sun size={15} className="text-amber-400" /> : <Moon size={15} className="text-indigo-400" />}
          <span className="hidden sm:inline">{t(mapTheme === 'dark' ? 'mapLightTheme' : 'mapDarkTheme')}</span>
        </button>

        {/* Language Switcher */}
        <button
          onClick={toggleLanguage}
          className="flex items-center space-x-1.5 space-x-reverse rounded-xl border border-gray-800 bg-[#0B0F19] px-3 py-1.5 text-xs font-semibold text-gray-300 hover:border-gray-700 hover:text-white transition"
        >
          <Globe size={15} className="text-cyan-400" />
          <span>{t('switchLanguage')}</span>
        </button>

        {/* Settings / Backend Config */}
        <button
          onClick={onOpenSettings}
          title={t('navSettings')}
          className="rounded-xl border border-gray-800 bg-[#0B0F19] p-2 text-gray-400 hover:border-cyan-500/40 hover:text-cyan-300 transition"
        >
          <Settings size={18} />
        </button>

        {/* Notifications Bell */}
        <div className="relative">
          <button
            onClick={onOpenAlerts}
            className="rounded-xl border border-gray-800 bg-[#0B0F19] p-2 text-gray-300 hover:text-white transition"
          >
            <Bell size={18} />
            {unreadAlerts > 0 && (
              <span className="absolute -top-1 -right-1 flex h-4 min-w-4 px-1 items-center justify-center rounded-full bg-rose-500 text-[10px] font-bold text-white ring-2 ring-[#050811]">
                {unreadAlerts > 99 ? '99+' : unreadAlerts}
              </span>
            )}
          </button>
        </div>

        {/* User Profile */}
        {user ? (
          <div className="flex items-center space-x-2 space-x-reverse border-r border-gray-800 pr-3 mr-1">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-tr from-cyan-500 to-blue-600 text-xs font-bold text-black ring-2 ring-cyan-500/30">
              {user.userName.charAt(0).toUpperCase()}
            </div>
            <div className="hidden sm:block text-right">
              <div className="text-xs font-semibold text-white leading-none">{user.userName}</div>
              <div className="text-[10px] text-cyan-400 font-mono mt-0.5">{user.role}</div>
            </div>
            <button
              onClick={logout}
              title={t('logout')}
              className="rounded-lg p-1.5 text-gray-400 hover:bg-rose-500/10 hover:text-rose-400 transition"
            >
              <LogOut size={16} />
            </button>
          </div>
        ) : null}
      </div>
    </header>
  );
};
