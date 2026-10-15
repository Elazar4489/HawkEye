import React, { useState } from 'react';
import { Eye, Shield, Lock, Mail, User as UserIcon, Phone, BellRing, CheckCircle, AlertCircle } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { useLanguage } from '../context/LanguageContext';

export const AuthPage: React.FC = () => {
  const { t } = useLanguage();
  const { login, register, requestPushPermission, pushEnabled, pushSupported } = useAuth();

  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [username, setUsername] = useState('');
  const [phone, setPhone] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [pushStatus, setPushStatus] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);

    setIsSubmitting(true);
    try {
      const error = mode === 'login'
        ? await login(email, password)
        : await register(email, username, password, phone);
      if (error) {
        setErrorMsg(mode === 'login' ? `${t('authError')} (${error})` : error);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleEnablePush = async () => {
    if (!pushSupported) {
      setPushStatus('התראות דפדפן זמינות רק ב-HTTPS או ב-localhost. התראות בזמן אמת יוצגו בתוך המערכת.');
      return;
    }
    const granted = await requestPushPermission();
    setPushStatus(granted ? t('pushPermissionSuccess') : 'ההרשאה נדחתה בדפדפן');
  };

  return (
    <div className="min-h-screen w-full bg-[#050811] text-gray-100 flex items-center justify-center p-6 relative overflow-hidden">
      {/* Background Cyber Grid */}
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,rgba(0,240,255,0.06)_0,transparent_70%)] pointer-events-none" />

      <div className="relative z-10 w-full max-w-md rounded-3xl border border-cyan-500/30 bg-[#0B0F19]/90 p-8 shadow-2xl shadow-cyan-500/10 backdrop-blur-2xl">
        {/* Logo */}
        <div className="flex flex-col items-center text-center mb-8">
          <div className="rounded-2xl bg-cyan-500/10 p-3.5 text-cyan-400 border border-cyan-500/40 shadow-lg shadow-cyan-500/20 mb-3">
            <Eye size={36} className="animate-pulse" />
          </div>
          <h2 className="text-2xl font-black tracking-wider text-white font-mono">{t('appName')}</h2>
          <p className="text-xs text-gray-400 mt-1">{t('authTitle')}</p>
        </div>

        {/* Tab Selector */}
        <div className="grid grid-cols-2 gap-2 rounded-xl bg-[#050811] p-1.5 border border-gray-800 mb-6">
          <button
            onClick={() => setMode('login')}
            className={`rounded-lg py-2 text-xs font-bold transition ${
              mode === 'login'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                : 'text-gray-400 hover:text-white'
            }`}
          >
            {t('loginNow')}
          </button>
          <button
            onClick={() => setMode('register')}
            className={`rounded-lg py-2 text-xs font-bold transition ${
              mode === 'register'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                : 'text-gray-400 hover:text-white'
            }`}
          >
            {t('registerNow')}
          </button>
        </div>

        {errorMsg && (
          <div className="mb-4 flex items-center space-x-2 space-x-reverse rounded-xl border border-rose-500/40 bg-rose-500/10 p-3 text-xs text-rose-300">
            <AlertCircle size={16} />
            <span>{errorMsg}</span>
          </div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {mode === 'register' && (
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1">{t('usernameLabel')}</label>
              <div className="relative">
                <input
                  type="text"
                  required
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  className="w-full rounded-xl border border-gray-800 bg-[#050811] px-4 py-2.5 pl-10 text-xs text-white focus:border-cyan-500 focus:outline-none"
                  placeholder="הכנס שם משתמש"
                />
                <UserIcon size={16} className="absolute left-3 top-3 text-gray-500" />
              </div>
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">{t('emailLabel')}</label>
            <div className="relative">
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full rounded-xl border border-gray-800 bg-[#050811] px-4 py-2.5 pl-10 text-xs text-white focus:border-cyan-500 focus:outline-none"
                placeholder="name@hawkeye.com"
              />
              <Mail size={16} className="absolute left-3 top-3 text-gray-500" />
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">{t('passwordLabel')}</label>
            <div className="relative">
              <input
                type="password"
                required
                minLength={mode === 'register' ? 6 : undefined}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full rounded-xl border border-gray-800 bg-[#050811] px-4 py-2.5 pl-10 text-xs text-white focus:border-cyan-500 focus:outline-none"
                placeholder="••••••••"
              />
              <Lock size={16} className="absolute left-3 top-3 text-gray-500" />
            </div>
          </div>

          {mode === 'register' && (
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1">{t('phoneLabel')}</label>
              <div className="relative">
                <input
                  type="tel"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  className="w-full rounded-xl border border-gray-800 bg-[#050811] px-4 py-2.5 pl-10 text-xs text-white focus:border-cyan-500 focus:outline-none"
                  placeholder="050-0000000"
                />
                <Phone size={16} className="absolute left-3 top-3 text-gray-500" />
              </div>
            </div>
          )}

          {/* Browser Push Permission Request Prompt */}
          <div className="pt-2">
            <button
              type="button"
              onClick={handleEnablePush}
              className={`w-full flex items-center justify-center space-x-2 space-x-reverse rounded-xl border p-3 text-xs font-semibold transition ${
                pushEnabled
                  ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-400'
                  : 'border-cyan-500/30 bg-cyan-500/10 text-cyan-300 hover:bg-cyan-500/20'
              }`}
            >
              <BellRing size={16} />
              <span>{pushEnabled ? t('pushPermissionSuccess') : t('pushPermissionButton')}</span>
            </button>
            {pushStatus && !pushEnabled && (
              <p className="mt-2 text-[11px] text-gray-400 text-center">{pushStatus}</p>
            )}
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full disabled:opacity-60 rounded-xl bg-gradient-to-r from-cyan-500 to-blue-600 py-3 text-sm font-bold text-black hover:opacity-90 transition shadow-lg shadow-cyan-500/20"
          >
            {mode === 'login' ? t('enterSystem') : t('registerNow')}
          </button>
        </form>
      </div>
    </div>
  );
};
