import React, { useState } from 'react';
import { BarChart3, Plane, ShieldAlert, Activity, Play, ExternalLink } from 'lucide-react';
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend } from 'recharts';
import { useLanguage } from '../context/LanguageContext';
import { generateDashboardApi, getDashboardApi, getErrorMessage } from '../services/api';
import { DashboardDocument, DashboardRequest } from '../types';

const PIE_COLORS = ['#00F0FF', '#A855F7', '#3B82F6', '#FFC700', '#10B981', '#F97316', '#EC4899', '#64748B'];

const PRESETS: { label: string; area: Omit<DashboardRequest, 'dashboardName'> }[] = [
  { label: 'ישראל', area: { minLatitude: 29, maxLatitude: 34, minLongitude: 33, maxLongitude: 37 } },
  { label: 'מזרח הים התיכון', area: { minLatitude: 28, maxLatitude: 38, minLongitude: 25, maxLongitude: 40 } },
  { label: 'אירופה', area: { minLatitude: 35, maxLatitude: 60, minLongitude: -10, maxLongitude: 30 } },
  { label: 'כל העולם', area: { minLatitude: -90, maxLatitude: 90, minLongitude: -180, maxLongitude: 180 } },
];

const REASON_LABELS: Record<string, string> = {
  LowAltitude: 'גובה נמוך',
  RapidDescent: 'ירידה חדה',
  HighSpeed: 'מהירות גבוהה',
};

export const AnalyticsPage: React.FC = () => {
  const { t } = useLanguage();
  const [request, setRequest] = useState<DashboardRequest>({ dashboardName: 'ניתוח סיכונים - ישראל', ...PRESETS[0].area });
  const [dashboard, setDashboard] = useState<DashboardDocument | null>(null);
  const [lookupId, setLookupId] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = async (action: () => Promise<DashboardDocument>) => {
    setLoading(true);
    setError(null);
    try {
      setDashboard(await action());
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const setNumber = (key: keyof Omit<DashboardRequest, 'dashboardName'>) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setRequest((r) => ({ ...r, [key]: Number(e.target.value) }));

  const risk = dashboard?.risk;
  const riskColor = risk?.riskLevel === 'High' ? 'text-rose-400' : risk?.riskLevel === 'Medium' ? 'text-amber-400' : 'text-emerald-400';
  const kibanaUrl = `${window.location.protocol}//${window.location.hostname}:5601`;

  return (
    <div className="h-full w-full bg-[#050811] p-6 overflow-y-auto space-y-6 text-gray-100">
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center space-x-3 space-x-reverse">
          <div className="rounded-2xl bg-purple-500/10 p-3 text-purple-400 border border-purple-500/30">
            <BarChart3 size={28} />
          </div>
          <div>
            <h2 className="text-xl font-bold text-white">{t('analyticsTitle')}</h2>
            <p className="text-xs text-gray-400">דשבורד חד-פעמי לניתוח סיכונים: MongoDB ← ניתוח ← Elasticsearch / Kibana</p>
          </div>
        </div>
        <a
          href={kibanaUrl}
          target="_blank"
          rel="noreferrer"
          className="flex items-center gap-1.5 rounded-xl border border-purple-500/40 bg-purple-500/10 px-3 py-2 text-xs font-bold text-purple-300 hover:bg-purple-500/20"
        >
          <ExternalLink size={14} /> Kibana
        </a>
      </div>

      {/* Request form */}
      <div className="rounded-2xl border border-cyan-500/20 bg-[#0B0F19] p-5 space-y-4">
        <div className="flex flex-wrap gap-2">
          {PRESETS.map((preset) => (
            <button
              key={preset.label}
              onClick={() => setRequest((r) => ({ ...r, ...preset.area, dashboardName: `ניתוח סיכונים - ${preset.label}` }))}
              className="rounded-lg border border-gray-800 px-3 py-1.5 text-xs text-gray-300 hover:border-cyan-500/40 hover:text-cyan-300"
            >
              {preset.label}
            </button>
          ))}
        </div>
        <div className="grid grid-cols-2 md:grid-cols-6 gap-3 text-xs">
          <label className="md:col-span-2">
            <span className="block text-gray-400 mb-1">שם הדשבורד</span>
            <input
              value={request.dashboardName}
              onChange={(e) => setRequest((r) => ({ ...r, dashboardName: e.target.value }))}
              className="w-full rounded-lg border border-gray-800 bg-[#050811] px-3 py-2 text-white focus:border-cyan-500 focus:outline-none"
            />
          </label>
          {([
            ['minLatitude', 'Lat min'],
            ['maxLatitude', 'Lat max'],
            ['minLongitude', 'Lon min'],
            ['maxLongitude', 'Lon max'],
          ] as const).map(([key, label]) => (
            <label key={key}>
              <span className="block text-gray-400 mb-1 font-mono">{label}</span>
              <input
                type="number"
                step="0.5"
                value={request[key]}
                onChange={setNumber(key)}
                className="w-full rounded-lg border border-gray-800 bg-[#050811] px-3 py-2 font-mono text-white focus:border-cyan-500 focus:outline-none"
              />
            </label>
          ))}
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <button
            onClick={() => run(() => generateDashboardApi(request))}
            disabled={loading}
            className="flex items-center gap-2 rounded-xl bg-gradient-to-r from-purple-500 to-cyan-500 px-5 py-2.5 text-xs font-bold text-black hover:opacity-90 disabled:opacity-60"
          >
            <Play size={14} /> {loading ? 'מפיק דשבורד...' : 'הפק דשבורד'}
          </button>
          <div className="flex items-center gap-2 text-xs">
            <input
              value={lookupId}
              onChange={(e) => setLookupId(e.target.value)}
              placeholder="Dashboard ID"
              className="w-72 rounded-lg border border-gray-800 bg-[#050811] px-3 py-2 font-mono text-white focus:border-cyan-500 focus:outline-none"
            />
            <button
              onClick={() => lookupId.trim() && run(() => getDashboardApi(lookupId.trim()))}
              disabled={loading || !lookupId.trim()}
              className="rounded-lg border border-gray-800 px-3 py-2 text-gray-300 hover:text-white disabled:opacity-40"
            >
              טען
            </button>
          </div>
        </div>
        {error && <div className="rounded-lg border border-rose-500/40 bg-rose-500/10 p-2.5 text-xs text-rose-300">{error}</div>}
      </div>

      {!dashboard && !loading && (
        <div className="rounded-2xl border border-dashed border-gray-800 p-10 text-center text-xs text-gray-500">
          בחר אזור והפק דשבורד כדי לנתח את תמונת המצב האווירית העדכנית
        </div>
      )}

      {dashboard && risk && (
        <>
          <div className="text-[11px] font-mono text-gray-500">
            ID: {dashboard.id} · {new Date(dashboard.createdAt).toLocaleString()}
            {dashboard.snapshotRecordedAt && ` · snapshot ${new Date(dashboard.snapshotRecordedAt).toLocaleTimeString()}`}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
            <div className="rounded-2xl border border-cyan-500/20 bg-[#0B0F19] p-5">
              <div className="flex items-center justify-between text-xs text-gray-400 mb-2">
                <span>{t('totalToday')}</span>
                <Plane size={18} className="text-cyan-400" />
              </div>
              <div className="text-2xl font-bold font-mono text-white">{dashboard.totalFlights.toLocaleString()}</div>
              <div className="text-[11px] text-gray-500 mt-1">{risk.airborneCount} באוויר · {risk.onGroundCount} על הקרקע</div>
            </div>

            <div className="rounded-2xl border border-rose-500/20 bg-[#0B0F19] p-5">
              <div className="flex items-center justify-between text-xs text-gray-400 mb-2">
                <span>מדד סיכון</span>
                <ShieldAlert size={18} className="text-rose-400" />
              </div>
              <div className={`text-2xl font-bold font-mono ${riskColor}`}>{risk.riskScore}/100</div>
              <div className={`text-[11px] mt-1 ${riskColor}`}>{risk.riskLevel}</div>
            </div>

            <div className="rounded-2xl border border-amber-500/20 bg-[#0B0F19] p-5">
              <div className="flex items-center justify-between text-xs text-gray-400 mb-2">
                <span>חריגות</span>
                <Activity size={18} className="text-amber-400" />
              </div>
              <div className="text-xs space-y-1 font-mono">
                <div>גובה נמוך: <span className="text-amber-300 font-bold">{risk.lowAltitudeCount}</span></div>
                <div>ירידה חדה: <span className="text-amber-300 font-bold">{risk.rapidDescentCount}</span></div>
                <div>מהירות גבוהה: <span className="text-amber-300 font-bold">{risk.highSpeedCount}</span></div>
              </div>
            </div>

            <div className="rounded-2xl border border-emerald-500/20 bg-[#0B0F19] p-5">
              <div className="flex items-center justify-between text-xs text-gray-400 mb-2">
                <span>ממוצעים</span>
                <BarChart3 size={18} className="text-emerald-400" />
              </div>
              <div className="text-xs space-y-1 font-mono">
                <div>גובה: <span className="text-emerald-300 font-bold">{risk.averageAltitude.toLocaleString()} m</span></div>
                <div>מהירות: <span className="text-emerald-300 font-bold">{Math.round(risk.averageVelocity * 3.6)} km/h</span></div>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="rounded-2xl border border-gray-800 bg-[#0B0F19] p-5">
              <h3 className="text-sm font-bold text-white mb-4">פילוח לפי שכבות גובה</h3>
              <div className="h-64 w-full" dir="ltr">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={risk.altitudeBands}>
                    <XAxis dataKey="name" stroke="#6B7280" />
                    <YAxis stroke="#6B7280" allowDecimals={false} />
                    <Tooltip contentStyle={{ backgroundColor: '#050811', borderColor: '#00F0FF' }} />
                    <Bar dataKey="count" fill="#00F0FF" radius={[6, 6, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>

            <div className="rounded-2xl border border-gray-800 bg-[#0B0F19] p-5">
              <h3 className="text-sm font-bold text-white mb-4">פילוח טיסות לפי מדינות רישום</h3>
              <div className="h-64 w-full" dir="ltr">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie data={risk.countryBreakdown} dataKey="count" nameKey="name" cx="50%" cy="50%" outerRadius={80}>
                      {risk.countryBreakdown.map((entry, index) => (
                        <Cell key={entry.name} fill={PIE_COLORS[index % PIE_COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip contentStyle={{ backgroundColor: '#050811', borderColor: '#00F0FF' }} />
                    <Legend wrapperStyle={{ fontSize: 11 }} />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            </div>
          </div>

          <div className="rounded-2xl border border-gray-800 bg-[#0B0F19] overflow-x-auto">
            <h3 className="text-sm font-bold text-white p-5 pb-2">כלי טיס בסיכון</h3>
            <table className="w-full text-right text-xs">
              <thead className="bg-[#050811] text-gray-400 border-b border-gray-800">
                <tr>
                  <th className="p-3">ICAO / Callsign</th>
                  <th className="p-3">מדינה</th>
                  <th className="p-3">גובה (m)</th>
                  <th className="p-3">מהירות (km/h)</th>
                  <th className="p-3">קצב אנכי (m/s)</th>
                  <th className="p-3">סיבות</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-800/50">
                {risk.riskFlights.length === 0 && (
                  <tr><td colSpan={6} className="p-5 text-center text-gray-500">לא זוהו חריגות באזור</td></tr>
                )}
                {risk.riskFlights.map((f) => (
                  <tr key={f.icao24}>
                    <td className="p-3 font-mono text-cyan-400 font-bold">{f.callsign || '-'} ({f.icao24})</td>
                    <td className="p-3 text-gray-300">{f.originCountry || '-'}</td>
                    <td className="p-3 font-mono">{f.baroAltitude != null ? Math.round(f.baroAltitude) : '-'}</td>
                    <td className="p-3 font-mono">{f.velocity != null ? Math.round(f.velocity * 3.6) : '-'}</td>
                    <td className="p-3 font-mono">{f.verticalRate != null ? f.verticalRate.toFixed(1) : '-'}</td>
                    <td className="p-3">
                      {f.reasons.map((r) => (
                        <span key={r} className="ml-1 rounded-full bg-rose-500/15 px-2 py-0.5 text-[10px] text-rose-300 border border-rose-500/30">
                          {REASON_LABELS[r] ?? r}
                        </span>
                      ))}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
};
