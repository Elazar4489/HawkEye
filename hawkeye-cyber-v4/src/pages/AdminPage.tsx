import React, { useCallback, useEffect, useState } from 'react';
import { UserCog, Database, Cpu, Wifi, RefreshCw } from 'lucide-react';
import { getErrorMessage, getSystemHealthApi, getUsersListApi, updateUserRoleApi } from '../services/api';
import { SystemHealth, User } from '../types';
import { useLanguage } from '../context/LanguageContext';
import { useAuth } from '../context/AuthContext';

const statusColor = (status?: string) => (status === 'Online' ? 'text-emerald-400' : 'text-rose-400');

export const AdminPage: React.FC = () => {
  const { t } = useLanguage();
  const { user: currentUser } = useAuth();
  const [health, setHealth] = useState<SystemHealth | null>(null);
  const [users, setUsers] = useState<User[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [healthResult, usersResult] = await Promise.all([getSystemHealthApi(), getUsersListApi()]);
      setHealth(healthResult);
      setUsers(usersResult);
      setError(null);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
    const timer = setInterval(load, 10000);
    return () => clearInterval(timer);
  }, [load]);

  const toggleRole = async (target: User) => {
    try {
      await updateUserRoleApi(target.id, target.role === 'Admin' ? 'Operator' : 'Admin');
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const ingestion = health?.lastIngestionUtc
    ? `${health.lastSnapshotFlights.toLocaleString()} flights · ${new Date(health.lastIngestionUtc).toLocaleTimeString()}`
    : 'No data yet';

  return (
    <div className="h-full w-full bg-[#050811] p-6 overflow-y-auto space-y-6 text-gray-100">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3 space-x-reverse">
          <div className="rounded-2xl bg-amber-500/10 p-3 text-amber-400 border border-amber-500/30">
            <UserCog size={28} />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white">{t('adminTitle')}</h2>
            <p className="text-xs text-gray-400">ניהול משתמשים ב-MySQL ולוח בקרה לתשתיות ה-Backend</p>
          </div>
        </div>
        <button onClick={() => void load()} className="rounded-xl border border-gray-800 p-2 text-gray-300 hover:text-white">
          <RefreshCw size={16} className={loading ? 'animate-spin' : ''} />
        </button>
      </div>

      {error && <div className="rounded-xl border border-rose-500/40 bg-rose-500/10 p-3 text-xs text-rose-300">{error}</div>}

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="rounded-2xl border border-emerald-500/30 bg-[#0B0F19] p-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs text-gray-400">Kafka Broker Status</span>
            <Cpu size={18} className="text-emerald-400" />
          </div>
          <div className={`text-lg font-bold font-mono ${statusColor(health?.kafkaStatus)}`}>{health?.kafkaStatus ?? '...'}</div>
        </div>

        <div className="rounded-2xl border border-emerald-500/30 bg-[#0B0F19] p-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs text-gray-400">MySQL Spatial Engine</span>
            <Database size={18} className="text-emerald-400" />
          </div>
          <div className={`text-lg font-bold font-mono ${statusColor(health?.mySqlStatus)}`}>{health?.mySqlStatus ?? '...'}</div>
          <div className="text-[11px] text-gray-500 mt-1">{health ? `${health.totalUsers} users · ${health.totalZones} zones` : ''}</div>
        </div>

        <div className="rounded-2xl border border-purple-500/30 bg-[#0B0F19] p-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs text-gray-400">MongoDB Cluster</span>
            <Database size={18} className="text-purple-400" />
          </div>
          <div className={`text-lg font-bold font-mono ${statusColor(health?.mongoStatus)}`}>{health?.mongoStatus ?? '...'}</div>
        </div>

        <div className="rounded-2xl border border-cyan-500/30 bg-[#0B0F19] p-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs text-gray-400">OpenSky Ingestion</span>
            <Wifi size={18} className="text-cyan-400" />
          </div>
          <div className="text-sm font-bold text-cyan-300 font-mono">{ingestion}</div>
          <div className="text-[11px] text-gray-500 mt-1">{health ? `${health.activeSignalRConnections} live SignalR users` : ''}</div>
        </div>
      </div>

      <div className="rounded-2xl border border-gray-800 bg-[#0B0F19] p-6 space-y-4 overflow-x-auto">
        <h3 className="text-sm font-bold text-white">{t('userManagement')}</h3>
        <table className="w-full text-right text-xs">
          <thead className="bg-[#050811] text-gray-400 border-b border-gray-800">
            <tr>
              <th className="p-3">מזהה</th>
              <th className="p-3">שם משתמש</th>
              <th className="p-3">כתובת אימייל</th>
              <th className="p-3">תפקיד</th>
              <th className="p-3">סטטוס Push Token</th>
              <th className="p-3">פעולות</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-800">
            {users.map((u) => (
              <tr key={u.id}>
                <td className="p-3 font-mono text-gray-500">#{u.id}</td>
                <td className="p-3 font-bold text-white">{u.userName}</td>
                <td className="p-3 font-mono text-gray-300">{u.email}</td>
                <td className="p-3">
                  <span className={`rounded px-2 py-0.5 font-bold ${u.role === 'Admin' ? 'bg-amber-500/20 text-amber-300' : 'bg-cyan-500/20 text-cyan-300'}`}>
                    {u.role}
                  </span>
                </td>
                <td className={`p-3 font-mono ${u.hasPushToken ? 'text-emerald-400' : 'text-gray-500'}`}>
                  {u.hasPushToken ? 'registered' : 'none'}
                </td>
                <td className="p-3">
                  {u.id !== currentUser?.id && (
                    <button onClick={() => toggleRole(u)} className="text-xs text-cyan-400 hover:underline">
                      {u.role === 'Admin' ? 'הפוך ל-Operator' : 'הפוך ל-Admin'}
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
