import React, { useCallback, useEffect, useState } from 'react';
import { Bell, Download, Filter, RefreshCw } from 'lucide-react';
import { getAlertsHistoryApi, getErrorMessage, markAlertsReadApi } from '../services/api';
import { NotificationHistoryItem } from '../types';
import { useLanguage } from '../context/LanguageContext';
import { useAuth } from '../context/AuthContext';

interface Props {
  // Incremented by App on every real-time alert, so the table reloads
  refreshKey: number;
}

export const AlertsPage: React.FC<Props> = ({ refreshKey }) => {
  const { t } = useLanguage();
  const { user } = useAuth();
  const [history, setHistory] = useState<NotificationHistoryItem[]>([]);
  const [severityFilter, setSeverityFilter] = useState('ALL');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    try {
      setHistory(await getAlertsHistoryApi(user.id));
      setError(null);
      void markAlertsReadApi(user.id).catch(() => undefined);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    void load();
  }, [load, refreshKey]);

  const filteredHistory = history.filter(
    (item) => severityFilter === 'ALL' || item.severity.toUpperCase() === severityFilter
  );

  const exportCsv = () => {
    const header = ['timestamp', 'icao24', 'callsign', 'zone', 'channel', 'severity', 'latitude', 'longitude', 'message'];
    const escape = (value: unknown) => `"${String(value ?? '').replace(/"/g, '""')}"`;
    const rows = filteredHistory.map((r) =>
      [r.timestamp, r.icao24, r.callsign, r.zoneName, r.channel, r.severity, r.latitude, r.longitude, r.message]
        .map(escape)
        .join(',')
    );
    const blob = new Blob(['﻿' + [header.join(','), ...rows].join('\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `hawkeye-alerts-${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="h-full w-full bg-[#050811] p-6 overflow-y-auto space-y-6 text-gray-100">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="flex items-center space-x-3 space-x-reverse">
          <div className="rounded-2xl bg-rose-500/10 p-3 text-rose-400 border border-rose-500/30">
            <Bell size={28} />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white">{t('alertsTitle')}</h2>
            <p className="text-xs text-gray-400">היסטוריית אירועי חציית אזור שנשלחו ב-Push וב-SignalR WebSocket</p>
          </div>
        </div>

        <div className="flex gap-2">
          <button
            onClick={() => void load()}
            className="flex items-center space-x-2 space-x-reverse rounded-xl border border-gray-800 px-3 py-2.5 text-xs font-bold text-gray-300 hover:text-white transition"
          >
            <RefreshCw size={16} className={loading ? 'animate-spin' : ''} />
          </button>
          <button
            onClick={exportCsv}
            disabled={filteredHistory.length === 0}
            className="flex items-center space-x-2 space-x-reverse rounded-xl border border-cyan-500/40 bg-cyan-500/10 px-4 py-2.5 text-xs font-bold text-cyan-300 hover:bg-cyan-500/20 transition disabled:opacity-40"
          >
            <Download size={16} />
            <span>{t('exportHistory')}</span>
          </button>
        </div>
      </div>

      <div className="flex items-center space-x-4 space-x-reverse rounded-2xl border border-gray-800 bg-[#0B0F19] p-4 text-xs">
        <Filter size={16} className="text-cyan-400" />
        <span className="font-semibold text-gray-300">{t('filterSeverity')}:</span>
        {[
          { id: 'ALL', label: t('allSeverities'), active: 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40' },
          { id: 'HIGH', label: 'High', active: 'bg-rose-500/20 text-rose-300 border border-rose-500/40' },
          { id: 'MEDIUM', label: 'Medium', active: 'bg-amber-500/20 text-amber-300 border border-amber-500/40' },
          { id: 'LOW', label: 'Low', active: 'bg-yellow-500/20 text-yellow-300 border border-yellow-500/40' },
        ].map((option) => (
          <button
            key={option.id}
            onClick={() => setSeverityFilter(option.id)}
            className={`rounded-lg px-3 py-1 font-bold transition ${severityFilter === option.id ? option.active : 'text-gray-400'}`}
          >
            {option.label}
          </button>
        ))}
      </div>

      {error && (
        <div className="rounded-xl border border-rose-500/40 bg-rose-500/10 p-3 text-xs text-rose-300">{error}</div>
      )}

      <div className="rounded-2xl border border-gray-800 bg-[#0B0F19] overflow-x-auto">
        <table className="w-full text-right text-xs">
          <thead className="bg-[#050811] text-gray-400 border-b border-gray-800">
            <tr>
              <th className="p-4">זמן אירוע</th>
              <th className="p-4">כלי טיס (ICAO / Callsign)</th>
              <th className="p-4">שם אזור חצוי</th>
              <th className="p-4">ערוץ שליחה</th>
              <th className="p-4">רמת חומרה</th>
              <th className="p-4">קואורדינטות</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-800/50">
            {filteredHistory.length === 0 && (
              <tr>
                <td colSpan={6} className="p-6 text-center text-gray-500">
                  {loading ? 'טוען...' : 'אין עדיין התראות. הגדר אזור עניין והמתן למטוס שיחצה אותו.'}
                </td>
              </tr>
            )}
            {filteredHistory.map((row) => (
              <tr key={row.id} className="hover:bg-cyan-500/5 transition">
                <td className="p-4 font-mono text-gray-300">{new Date(row.timestamp).toLocaleString()}</td>
                <td className="p-4 font-mono font-bold text-cyan-400">
                  {row.callsign || '-'} ({row.icao24 || '-'})
                </td>
                <td className="p-4 font-semibold text-white">{row.zoneName || '-'}</td>
                <td className="p-4">
                  <span className="rounded-full bg-cyan-500/10 px-2.5 py-0.5 font-mono text-[10px] text-cyan-300 border border-cyan-500/20">
                    {row.channel}
                  </span>
                </td>
                <td className="p-4">
                  <span
                    className={`rounded-full px-2.5 py-0.5 font-bold text-[10px] ${
                      row.severity === 'High'
                        ? 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                        : row.severity === 'Medium'
                          ? 'bg-amber-500/20 text-amber-400 border border-amber-500/30'
                          : 'bg-yellow-500/20 text-yellow-400 border border-yellow-500/30'
                    }`}
                  >
                    {row.severity}
                  </span>
                </td>
                <td className="p-4 font-mono text-gray-400">
                  {row.latitude != null && row.longitude != null
                    ? `${row.latitude.toFixed(4)}, ${row.longitude.toFixed(4)}`
                    : '-'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
