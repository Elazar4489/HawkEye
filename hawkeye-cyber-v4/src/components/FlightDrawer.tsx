import React from 'react';
import { X, Plane, Navigation, Gauge, ArrowUpRight, Globe2, AlertTriangle, ShieldCheck } from 'lucide-react';
import { FlightState } from '../types';
import { useLanguage } from '../context/LanguageContext';

interface Props {
  flight: FlightState | null;
  onClose: () => void;
  onCreateZoneAround: (flight: FlightState) => void;
}

export const FlightDrawer: React.FC<Props> = ({ flight, onClose, onCreateZoneAround }) => {
  const { t } = useLanguage();

  if (!flight) return null;

  const fmt = (value: number | null, factor = 1) => (value == null ? '-' : Math.round(value * factor).toLocaleString());

  return (
    <div className="fixed inset-y-0 left-0 z-50 w-full max-w-sm border-r border-cyan-500/30 bg-[#0B0F19]/95 p-6 backdrop-blur-2xl shadow-2xl shadow-cyan-500/20 text-gray-100 overflow-y-auto animate-in slide-in-from-left duration-300">
      {/* Header */}
      <div className="flex items-center justify-between pb-4 border-b border-gray-800">
        <div className="flex items-center space-x-3 space-x-reverse">
          <div className="rounded-xl bg-cyan-500/10 p-3 text-cyan-400 border border-cyan-500/30">
            <Plane size={24} className="transform -rotate-45" />
          </div>
          <div>
            <h3 className="text-xl font-bold font-mono text-white">{flight.callsign || 'N/A'}</h3>
            <span className="text-xs font-mono text-cyan-400">ICAO24: {flight.icao24}</span>
          </div>
        </div>
        <button
          onClick={onClose}
          className="rounded-full p-2 text-gray-400 hover:bg-gray-800 hover:text-white transition"
        >
          <X size={20} />
        </button>
      </div>

      {/* Alert Status Banner */}
      <div className="my-4 rounded-xl border border-cyan-500/30 bg-cyan-500/10 p-3 flex items-center justify-between text-xs">
        <div className="flex items-center space-x-2 space-x-reverse text-cyan-300 font-semibold">
          <ShieldCheck size={16} />
          <span>סטטוס מעקב: פעיל (Live Ingestion)</span>
        </div>
        <span className="font-mono text-cyan-400 font-bold">{flight.onGround ? 'ON GROUND' : 'AIRBORNE'}</span>
      </div>

      {/* Telemetry Metrics Grid */}
      <div className="grid grid-cols-2 gap-3 my-6">
        <div className="rounded-xl border border-gray-800 bg-[#050811] p-3">
          <div className="flex items-center space-x-1.5 space-x-reverse text-xs text-gray-400 mb-1">
            <ArrowUpRight size={14} className="text-cyan-400" />
            <span>{t('altitude')}</span>
          </div>
          <div className="text-lg font-bold font-mono text-white">
            {fmt(flight.baroAltitude)} <span className="text-xs text-gray-400 font-sans">m</span>
          </div>
        </div>

        <div className="rounded-xl border border-gray-800 bg-[#050811] p-3">
          <div className="flex items-center space-x-1.5 space-x-reverse text-xs text-gray-400 mb-1">
            <Gauge size={14} className="text-emerald-400" />
            <span>{t('velocity')}</span>
          </div>
          <div className="text-lg font-bold font-mono text-white">
            {fmt(flight.velocity, 1.944)} <span className="text-xs text-gray-400 font-sans">kts</span>
          </div>
        </div>

        <div className="rounded-xl border border-gray-800 bg-[#050811] p-3">
          <div className="flex items-center space-x-1.5 space-x-reverse text-xs text-gray-400 mb-1">
            <Navigation size={14} className="text-amber-400" />
            <span>{t('heading')}</span>
          </div>
          <div className="text-lg font-bold font-mono text-white">{fmt(flight.trueTrack)}°</div>
        </div>

        <div className="rounded-xl border border-gray-800 bg-[#050811] p-3">
          <div className="flex items-center space-x-1.5 space-x-reverse text-xs text-gray-400 mb-1">
            <Globe2 size={14} className="text-purple-400" />
            <span>{t('country')}</span>
          </div>
          <div className="text-sm font-semibold text-white truncate">{flight.originCountry}</div>
        </div>
      </div>

      {/* Coordinates */}
      <div className="mb-6 rounded-xl border border-gray-800 bg-[#050811] p-3 font-mono text-xs text-gray-300 space-y-1">
        <div className="flex justify-between">
          <span className="text-gray-500">Latitude:</span>
          <span className="text-cyan-400">{flight.latitude.toFixed(4)}°</span>
        </div>
        <div className="flex justify-between">
          <span className="text-gray-500">Longitude:</span>
          <span className="text-cyan-400">{flight.longitude.toFixed(4)}°</span>
        </div>
      </div>

      {/* Actions */}
      <div className="space-y-3">
        <button
          onClick={() => onCreateZoneAround(flight)}
          className="w-full flex items-center justify-center space-x-2 space-x-reverse rounded-xl border border-rose-500/40 bg-rose-500/10 px-4 py-3 text-xs font-bold text-rose-300 hover:bg-rose-500/20 transition"
        >
          <AlertTriangle size={16} />
          <span>הגדר אזור התראה מסביב למטוס זה</span>
        </button>
      </div>
    </div>
  );
};
