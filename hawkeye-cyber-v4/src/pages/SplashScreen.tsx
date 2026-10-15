import React from 'react';
import { Eye, Shield, Cpu, Activity, ArrowLeft, ArrowRight, Zap, Database, Lock } from 'lucide-react';
import { useLanguage } from '../context/LanguageContext';

interface Props {
  onEnterAuth: () => void;
}

export const SplashScreen: React.FC<Props> = ({ onEnterAuth }) => {
  const { t, dir } = useLanguage();

  return (
    <div className="relative min-h-screen w-full bg-[#050811] text-gray-100 flex flex-col justify-between overflow-hidden selection:bg-cyan-500 selection:text-black">
      {/* Background Cyber Radar Grid Effect */}
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,rgba(0,240,255,0.08)_0,transparent_70%)] pointer-events-none" />
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] rounded-full border border-cyan-500/10 pointer-events-none animate-ping duration-1000 opacity-20" />

      {/* Top Header Logo */}
      <header className="relative z-10 flex items-center justify-between p-8 max-w-7xl mx-auto w-full">
        <div className="flex items-center space-x-3 space-x-reverse">
          <div className="relative flex items-center justify-center rounded-2xl bg-cyan-500/10 p-3 text-cyan-400 border border-cyan-500/40 shadow-lg shadow-cyan-500/20">
            <Eye size={32} className="animate-pulse" />
          </div>
          <div>
            <h1 className="text-2xl font-black tracking-widest font-mono bg-gradient-to-r from-cyan-400 via-blue-400 to-indigo-500 bg-clip-text text-transparent">
              HAWKEYE
            </h1>
            <p className="text-xs font-semibold text-cyan-400/80 tracking-wide">Real-Time Flight Tracking & Spatial Alerts</p>
          </div>
        </div>

        <div className="hidden sm:flex items-center space-x-2 space-x-reverse rounded-full bg-cyan-500/10 px-4 py-1.5 border border-cyan-500/30 text-cyan-300 text-xs font-mono">
          <Shield size={14} />
          <span>PROTOTYPE v4.0 .NET 9</span>
        </div>
      </header>

      {/* Main Hero Content */}
      <main className="relative z-10 max-w-5xl mx-auto px-6 text-center space-y-8 my-auto">
        <div className="inline-flex items-center space-x-2 space-x-reverse rounded-full border border-cyan-500/30 bg-cyan-500/10 px-4 py-1.5 text-xs font-bold text-cyan-400 shadow-lg shadow-cyan-500/10">
          <Zap size={14} className="animate-bounce" />
          <span>מערכת ניטור אסטרטגית בזמן אמת מבוססת Event Streaming</span>
        </div>

        <h2 className="text-4xl sm:text-6xl font-extrabold tracking-tight text-white leading-tight">
          {t('splashTitle')}
        </h2>

        <p className="max-w-2xl mx-auto text-base sm:text-lg text-gray-300 leading-relaxed font-light">
          {t('splashSubtitle')}
        </p>

        {/* Action Button to Auth */}
        <div className="pt-4">
          <button
            onClick={onEnterAuth}
            className="group relative inline-flex items-center justify-center space-x-3 space-x-reverse rounded-2xl bg-gradient-to-r from-cyan-500 via-blue-600 to-indigo-600 px-8 py-4 text-lg font-extrabold text-black hover:opacity-95 transition shadow-2xl shadow-cyan-500/30 hover:scale-105 active:scale-95"
          >
            <span>{t('splashCTA')}</span>
            {dir === 'rtl' ? <ArrowLeft size={22} className="group-hover:-translate-x-1 transition" /> : <ArrowRight size={22} className="group-hover:translate-x-1 transition" />}
          </button>
        </div>

        {/* Feature Cards Grid */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-12 text-right">
          <div className="rounded-2xl border border-cyan-500/20 bg-[#0B0F19]/80 p-6 backdrop-blur-xl hover:border-cyan-500/50 transition group">
            <div className="rounded-xl bg-cyan-500/10 p-3 text-cyan-400 border border-cyan-500/30 w-fit mb-4 group-hover:scale-110 transition">
              <Activity size={24} />
            </div>
            <h3 className="text-lg font-bold text-white mb-2">{t('splashFeature1Title')}</h3>
            <p className="text-xs text-gray-400 leading-relaxed">{t('splashFeature1Desc')}</p>
          </div>

          <div className="rounded-2xl border border-cyan-500/20 bg-[#0B0F19]/80 p-6 backdrop-blur-xl hover:border-cyan-500/50 transition group">
            <div className="rounded-xl bg-emerald-500/10 p-3 text-emerald-400 border border-emerald-500/30 w-fit mb-4 group-hover:scale-110 transition">
              <Database size={24} />
            </div>
            <h3 className="text-lg font-bold text-white mb-2">{t('splashFeature2Title')}</h3>
            <p className="text-xs text-gray-400 leading-relaxed">{t('splashFeature2Desc')}</p>
          </div>

          <div className="rounded-2xl border border-cyan-500/20 bg-[#0B0F19]/80 p-6 backdrop-blur-xl hover:border-cyan-500/50 transition group">
            <div className="rounded-xl bg-rose-500/10 p-3 text-rose-400 border border-rose-500/30 w-fit mb-4 group-hover:scale-110 transition">
              <Lock size={24} />
            </div>
            <h3 className="text-lg font-bold text-white mb-2">{t('splashFeature3Title')}</h3>
            <p className="text-xs text-gray-400 leading-relaxed">{t('splashFeature3Desc')}</p>
          </div>
        </div>
      </main>

      {/* Footer info */}
      <footer className="relative z-10 p-6 text-center text-xs text-gray-500 font-mono">
        HawkEye System Architecture • OpenSky Feed + Kafka + MySQL Spatial + SignalR WebSocket
      </footer>
    </div>
  );
};
