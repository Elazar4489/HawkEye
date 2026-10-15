import React, { useEffect, useState } from 'react';
import { X, Server, Database, Cpu, Wifi, CheckCircle, AlertTriangle, RotateCcw } from 'lucide-react';
import { BackendConfig, defaultConfig, getStoredConfig, getSystemHealthApi, pingApi, saveStoredConfig } from '../services/api';
import { SystemHealth } from '../types';
import { useLanguage } from '../context/LanguageContext';

interface Props {
  isOpen: boolean;
  onClose: () => void;
}

const statusClass = (status?: string) =>
  status === 'Online' ? 'text-emerald-400' : status ? 'text-rose-400' : 'text-gray-500';

export const BackendConfigModal: React.FC<Props> = ({ isOpen, onClose }) => {
  const { t } = useLanguage();
  const [config, setConfig] = useState<BackendConfig>(getStoredConfig);
  const [testResult, setTestResult] = useState<{ api: boolean; dashboard: boolean } | null>(null);
  const [health, setHealth] = useState<SystemHealth | null>(null);
  const [testing, setTesting] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setConfig(getStoredConfig());
      setTestResult(null);
      getSystemHealthApi().then(setHealth).catch(() => setHealth(null));
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleTestConnection = async () => {
    setTesting(true);
    try {
      setTestResult(await pingApi(config));
    } finally {
      setTesting(false);
    }
  };

  const handleSave = () => {
    saveStoredConfig(config);
    onClose();
    // SignalR and every page pick the new URLs up on reload
    window.location.reload();
  };

  const field = (key: keyof BackendConfig, label: string) => (
    <div>
      <label className="block text-xs font-mono text-gray-400 mb-1">{label}</label>
      <input
        type="text"
        value={config[key]}
        onChange={(e) => setConfig((c) => ({ ...c, [key]: e.target.value }))}
        className="w-full rounded-lg border border-cyan-500/30 bg-[#050811] px-4 py-2.5 font-mono text-sm text-cyan-300 focus:border-cyan-400 focus:outline-none"
        dir="ltr"
      />
    </div>
  );

  const allOk = testResult?.api && testResult?.dashboard;

  return (
    <div className="fixed inset-0 z-[2000] flex items-center justify-center bg-black/80 backdrop-blur-md p-4">
      <div className="relative w-full max-w-2xl rounded-2xl border border-cyan-500/40 bg-[#0B0F19]/95 p-6 shadow-2xl shadow-cyan-500/10 text-gray-100">
        <button
          onClick={onClose}
          className="absolute left-4 top-4 rounded-full p-2 text-gray-400 hover:bg-gray-800 hover:text-white transition"
        >
          <X size={20} />
        </button>

        <div className="flex items-center space-x-3 space-x-reverse mb-6">
          <div className="rounded-xl bg-cyan-500/10 p-3 text-cyan-400 border border-cyan-500/30">
            <Server size={26} />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white flex items-center gap-2">
              {t('navSettings')} (Backend & DB Connections)
            </h2>
            <p className="text-xs text-gray-400">
              ברירת המחדל היא נתיבים יחסיים דרך ה-Proxy, כך שהמערכת עובדת גם מ-localhost וגם מכתובת IP ברשת
            </p>
          </div>
        </div>

        <div className="space-y-4 mb-6">
          {field('authApiUrl', 'FlightTracking API (auth, zones, alerts, flights)')}
          {field('dashboardApiUrl', 'Dashboard API (risk dashboards)')}
          {field('signalRHubUrl', 'SignalR Hub')}
        </div>

        <div className="grid grid-cols-3 gap-3 mb-6">
          <div className="rounded-xl border border-emerald-500/30 bg-emerald-500/5 p-3 text-center">
            <Database size={18} className="mx-auto mb-1 text-emerald-400" />
            <div className="text-xs text-gray-400">MySQL Spatial SQL</div>
            <div className={`text-xs font-bold mt-0.5 ${statusClass(health?.mySqlStatus)}`}>{health?.mySqlStatus ?? '-'}</div>
          </div>
          <div className="rounded-xl border border-emerald-500/30 bg-emerald-500/5 p-3 text-center">
            <Cpu size={18} className="mx-auto mb-1 text-emerald-400" />
            <div className="text-xs text-gray-400">Kafka Broker</div>
            <div className={`text-xs font-bold mt-0.5 ${statusClass(health?.kafkaStatus)}`}>{health?.kafkaStatus ?? '-'}</div>
          </div>
          <div className="rounded-xl border border-purple-500/30 bg-purple-500/5 p-3 text-center">
            <Database size={18} className="mx-auto mb-1 text-purple-400" />
            <div className="text-xs text-gray-400">MongoDB Cluster</div>
            <div className={`text-xs font-bold mt-0.5 ${statusClass(health?.mongoStatus)}`}>{health?.mongoStatus ?? '-'}</div>
          </div>
        </div>

        {testResult && (
          <div
            className={`mb-4 flex items-center space-x-2 space-x-reverse rounded-lg border p-3 text-sm ${
              allOk
                ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-300'
                : 'border-rose-500/40 bg-rose-500/10 text-rose-300'
            }`}
          >
            {allOk ? <CheckCircle size={18} /> : <AlertTriangle size={18} />}
            <span>
              API: {testResult.api ? 'OK' : 'FAILED'} · Dashboard API: {testResult.dashboard ? 'OK' : 'FAILED'}
            </span>
          </div>
        )}

        <div className="flex items-center justify-end space-x-3 space-x-reverse">
          <button
            type="button"
            onClick={() => setConfig(defaultConfig)}
            className="flex items-center gap-1 rounded-xl border border-gray-800 px-4 py-2.5 text-sm text-gray-300 hover:text-white transition"
          >
            <RotateCcw size={14} /> ברירת מחדל
          </button>
          <button
            type="button"
            onClick={handleTestConnection}
            disabled={testing}
            className="flex items-center gap-1 rounded-xl border border-cyan-500/40 bg-cyan-500/10 px-4 py-2.5 text-sm font-semibold text-cyan-300 hover:bg-cyan-500/20 transition disabled:opacity-60"
          >
            <Wifi size={14} /> בדוק חיבור שרת (Ping API)
          </button>
          <button
            type="button"
            onClick={handleSave}
            className="rounded-xl bg-gradient-to-r from-cyan-500 to-blue-600 px-6 py-2.5 text-sm font-bold text-black hover:opacity-90 transition shadow-lg shadow-cyan-500/20"
          >
            שמור והמשך
          </button>
        </div>
      </div>
    </div>
  );
};
