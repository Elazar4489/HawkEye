import React, { useCallback, useEffect, useState } from 'react';
import { MapContainer, TileLayer, Circle, Polygon, Polyline, CircleMarker, useMapEvents } from 'react-leaflet';
import { ShieldAlert, Save, Trash2, Undo2, MousePointerClick } from 'lucide-react';
import { createZoneApi, deleteZoneApi, getErrorMessage, getZonesApi } from '../services/api';
import { Coordinate, PriorityZone, Severity, priorityToSeverity, severityColor, severityToPriority } from '../types';
import { useLanguage } from '../context/LanguageContext';
import { useTheme } from '../context/ThemeContext';
import { useAuth } from '../context/AuthContext';
import { ZoneShapes, tileLayerFor } from '../components/ZoneShapes';

const MapClickHandler: React.FC<{ onClick: (point: Coordinate) => void }> = ({ onClick }) => {
  useMapEvents({
    click: (e) => onClick({ latitude: e.latlng.lat, longitude: e.latlng.lng }),
  });
  return null;
};

export const PriorityZonesPage: React.FC = () => {
  const { t } = useLanguage();
  const { mapTheme } = useTheme();
  const { user } = useAuth();

  const [zones, setZones] = useState<PriorityZone[]>([]);
  const [zoneType, setZoneType] = useState<'RADIUS' | 'POLYGON'>('RADIUS');
  const [zoneName, setZoneName] = useState('אזור הגנה אסטרטגי חדש');
  const [radius, setRadius] = useState(20000);
  const [priority, setPriority] = useState<Severity>('High');
  const [center, setCenter] = useState<Coordinate>({ latitude: 32.0094, longitude: 34.8854 });
  const [polygonPoints, setPolygonPoints] = useState<Coordinate[]>([]);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; ok: boolean } | null>(null);

  const loadZones = useCallback(async () => {
    if (!user) return;
    try {
      setZones(await getZonesApi(user.id));
    } catch (err) {
      setMessage({ text: getErrorMessage(err), ok: false });
    }
  }, [user]);

  useEffect(() => {
    void loadZones();
  }, [loadZones]);

  const handleMapClick = (point: Coordinate) => {
    if (zoneType === 'RADIUS') {
      setCenter(point);
    } else {
      setPolygonPoints((points) => [...points, point]);
    }
  };

  const handleSaveZone = async () => {
    if (!user) return;
    if (zoneType === 'POLYGON' && polygonPoints.length < 3) {
      setMessage({ text: 'יש לסמן לפחות 3 נקודות על המפה עבור פוליגון', ok: false });
      return;
    }

    setSaving(true);
    try {
      await createZoneApi(user.id, {
        name: zoneName,
        priority: severityToPriority(priority),
        areaType: zoneType,
        ...(zoneType === 'RADIUS'
          ? { centerLatitude: center.latitude, centerLongitude: center.longitude, radiusMeters: radius }
          : { polygonCoordinates: polygonPoints }),
      });
      setPolygonPoints([]);
      setMessage({ text: 'האזור נשמר בהצלחה במסד הנתונים MySQL Spatial!', ok: true });
      await loadZones();
    } catch (err) {
      setMessage({ text: getErrorMessage(err), ok: false });
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteZone = async (id: number) => {
    if (!user) return;
    try {
      await deleteZoneApi(user.id, id);
      setZones((current) => current.filter(z => z.id !== id));
    } catch (err) {
      setMessage({ text: getErrorMessage(err), ok: false });
    }
  };

  const draftColor = severityColor(priority);
  const tiles = tileLayerFor(mapTheme);

  return (
    <div className="h-full w-full flex flex-col lg:flex-row bg-[#050811] overflow-hidden text-gray-100">
      <div className="w-full lg:w-96 flex-shrink-0 border-l border-cyan-500/20 bg-[#0B0F19]/90 p-6 overflow-y-auto space-y-6 backdrop-blur-xl">
        <div className="flex items-center space-x-3 space-x-reverse">
          <div className="rounded-xl bg-emerald-500/10 p-2.5 text-emerald-400 border border-emerald-500/30">
            <ShieldAlert size={24} />
          </div>
          <div>
            <h2 className="text-lg font-bold text-white">{t('zonesTitle')}</h2>
            <p className="text-xs text-gray-400">הגדרת גבולות גיאוגרפיים ב-MySQL NetTopologySuite</p>
          </div>
        </div>

        <div className="rounded-2xl border border-cyan-500/30 bg-[#050811] p-4 space-y-4">
          <div className="grid grid-cols-2 gap-2">
            <button
              onClick={() => setZoneType('RADIUS')}
              className={`rounded-xl py-2 text-xs font-bold transition ${
                zoneType === 'RADIUS'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/50'
                  : 'bg-gray-900 text-gray-400 border border-gray-800'
              }`}
            >
              {t('createRadiusZone')}
            </button>
            <button
              onClick={() => setZoneType('POLYGON')}
              className={`rounded-xl py-2 text-xs font-bold transition ${
                zoneType === 'POLYGON'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/50'
                  : 'bg-gray-900 text-gray-400 border border-gray-800'
              }`}
            >
              {t('createPolygonZone')}
            </button>
          </div>

          <div className="flex items-start gap-2 rounded-lg bg-cyan-500/5 border border-cyan-500/20 p-2.5 text-[11px] text-cyan-200">
            <MousePointerClick size={14} className="mt-0.5 flex-shrink-0" />
            <span>
              {zoneType === 'RADIUS'
                ? 'לחץ על המפה כדי לבחור את מרכז האזור'
                : `לחץ על המפה כדי להוסיף קודקודים (${polygonPoints.length} נבחרו, נדרשים לפחות 3)`}
            </span>
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">{t('zoneNameLabel')}</label>
            <input
              type="text"
              value={zoneName}
              maxLength={100}
              onChange={(e) => setZoneName(e.target.value)}
              className="w-full rounded-xl border border-gray-800 bg-[#0B0F19] px-3.5 py-2 text-xs text-white focus:border-cyan-500 focus:outline-none"
            />
          </div>

          {zoneType === 'RADIUS' ? (
            <div>
              <div className="flex justify-between text-xs text-gray-300 mb-1">
                <span>{t('radiusLabel')}</span>
                <span className="font-mono text-cyan-400 font-bold">{(radius / 1000).toFixed(1)} km</span>
              </div>
              <input
                type="range"
                min="2000"
                max="200000"
                step="1000"
                value={radius}
                onChange={(e) => setRadius(Number(e.target.value))}
                className="w-full accent-cyan-400 bg-gray-800 h-1.5 rounded-lg"
              />
              <div className="mt-1 text-[10px] font-mono text-gray-500">
                {center.latitude.toFixed(4)}, {center.longitude.toFixed(4)}
              </div>
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-2">
              <button
                onClick={() => setPolygonPoints((points) => points.slice(0, -1))}
                disabled={polygonPoints.length === 0}
                className="flex items-center justify-center gap-1 rounded-xl border border-gray-800 py-2 text-xs text-gray-300 hover:text-white disabled:opacity-40"
              >
                <Undo2 size={14} /> בטל נקודה
              </button>
              <button
                onClick={() => setPolygonPoints([])}
                disabled={polygonPoints.length === 0}
                className="flex items-center justify-center gap-1 rounded-xl border border-gray-800 py-2 text-xs text-gray-300 hover:text-white disabled:opacity-40"
              >
                <Trash2 size={14} /> נקה
              </button>
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">{t('priorityLabel')}</label>
            <select
              value={priority}
              onChange={(e) => setPriority(e.target.value as Severity)}
              className="w-full rounded-xl border border-gray-800 bg-[#0B0F19] px-3.5 py-2 text-xs text-white focus:border-cyan-500 focus:outline-none"
            >
              <option value="High">High Priority (אדום קריטי)</option>
              <option value="Medium">Medium Priority (כתום בינוני)</option>
              <option value="Low">Low Priority (צהוב מופחת)</option>
            </select>
          </div>

          {message && (
            <div className={`rounded-lg border p-2.5 text-xs ${message.ok ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-300' : 'border-rose-500/40 bg-rose-500/10 text-rose-300'}`}>
              {message.text}
            </div>
          )}

          <button
            onClick={handleSaveZone}
            disabled={saving}
            className="w-full flex items-center justify-center space-x-2 space-x-reverse rounded-xl bg-gradient-to-r from-emerald-500 to-teal-600 py-2.5 text-xs font-bold text-black hover:opacity-90 transition shadow-lg shadow-emerald-500/20 disabled:opacity-60"
          >
            <Save size={16} />
            <span>{t('saveZone')}</span>
          </button>
        </div>

        <div className="space-y-3">
          <h3 className="text-xs font-bold uppercase tracking-wider text-gray-400">{t('activeZonesList')}</h3>
          {zones.length === 0 && <div className="text-xs text-gray-500">אין עדיין אזורים מוגדרים</div>}
          {zones.map((z) => (
            <div
              key={z.id}
              className="flex items-center justify-between rounded-xl border border-gray-800 bg-[#050811] p-3 text-xs"
            >
              <div>
                <div className="font-semibold text-white flex items-center gap-2">
                  <span className="h-2 w-2 rounded-full" style={{ backgroundColor: severityColor(priorityToSeverity(z.priority)) }} />
                  {z.name || `Zone #${z.id}`}
                </div>
                <div className="text-[10px] text-cyan-400 font-mono mt-0.5">
                  {z.areaType} | {z.areaType === 'RADIUS' && z.radiusMeters ? `${(z.radiusMeters / 1000).toFixed(1)}km` : `${z.polygonCoordinates?.length ?? 0} pts`} | {priorityToSeverity(z.priority)}
                </div>
              </div>
              <button
                onClick={() => handleDeleteZone(z.id)}
                className="rounded-lg p-1.5 text-gray-500 hover:bg-rose-500/10 hover:text-rose-400 transition"
              >
                <Trash2 size={16} />
              </button>
            </div>
          ))}
        </div>
      </div>

      <div className="flex-1 h-full z-10">
        <MapContainer center={[center.latitude, center.longitude]} zoom={8} zoomControl={false} className="w-full h-full cursor-crosshair">
          <TileLayer key={mapTheme} url={tiles.url} attribution={tiles.attribution} className={tiles.className} />
          <MapClickHandler onClick={handleMapClick} />

          <ZoneShapes zones={zones} />

          {zoneType === 'RADIUS' && (
            <Circle
              center={[center.latitude, center.longitude]}
              radius={radius}
              pathOptions={{ color: draftColor, fillColor: draftColor, fillOpacity: 0.2, weight: 2, dashArray: '6 6' }}
            />
          )}

          {zoneType === 'POLYGON' && polygonPoints.length > 0 && (
            <>
              {polygonPoints.length >= 3 ? (
                <Polygon
                  positions={polygonPoints.map((p) => [p.latitude, p.longitude] as [number, number])}
                  pathOptions={{ color: draftColor, fillColor: draftColor, fillOpacity: 0.2, weight: 2, dashArray: '6 6' }}
                />
              ) : (
                <Polyline
                  positions={polygonPoints.map((p) => [p.latitude, p.longitude] as [number, number])}
                  pathOptions={{ color: draftColor, weight: 2, dashArray: '6 6' }}
                />
              )}
              {polygonPoints.map((p, i) => (
                <CircleMarker key={i} center={[p.latitude, p.longitude]} radius={4} pathOptions={{ color: draftColor, fillOpacity: 1 }} />
              ))}
            </>
          )}
        </MapContainer>
      </div>
    </div>
  );
};
