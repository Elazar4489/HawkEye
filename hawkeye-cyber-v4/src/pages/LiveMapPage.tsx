import React, { useCallback, useEffect, useState } from 'react';
import { MapContainer, TileLayer, CircleMarker, Popup, useMapEvents } from 'react-leaflet';
import { Search, RefreshCw, AlertTriangle } from 'lucide-react';
import { createZoneApi, getErrorMessage, getLiveFlightsApi, getZonesApi } from '../services/api';
import { FlightState, MapBounds, PriorityZone } from '../types';
import { useTheme } from '../context/ThemeContext';
import { useLanguage } from '../context/LanguageContext';
import { useAuth } from '../context/AuthContext';
import { FlightDrawer } from '../components/FlightDrawer';
import { ZoneShapes, tileLayerFor } from '../components/ZoneShapes';

const REFRESH_MS = 15000;

// Reports the visible map area so only flights inside it are requested.
const BoundsWatcher: React.FC<{ onChange: (bounds: MapBounds) => void }> = ({ onChange }) => {
  const map = useMapEvents({
    moveend: () => report(),
    zoomend: () => report(),
  });

  const report = useCallback(() => {
    const b = map.getBounds();
    onChange({
      minLat: Math.max(-90, b.getSouth()),
      maxLat: Math.min(90, b.getNorth()),
      minLon: Math.max(-180, b.getWest()),
      maxLon: Math.min(180, b.getEast()),
    });
  }, [map, onChange]);

  useEffect(() => {
    report();
  }, [report]);

  return null;
};

export const LiveMapPage: React.FC = () => {
  const { t } = useLanguage();
  const { mapTheme } = useTheme();
  const { user } = useAuth();
  const [flights, setFlights] = useState<FlightState[]>([]);
  const [zones, setZones] = useState<PriorityZone[]>([]);
  const [bounds, setBounds] = useState<MapBounds | null>(null);
  const [snapshotTime, setSnapshotTime] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [selectedFlight, setSelectedFlight] = useState<FlightState | null>(null);

  const loadZones = useCallback(async () => {
    if (!user) return;
    try {
      setZones(await getZonesApi(user.id));
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }, [user]);

  useEffect(() => {
    void loadZones();
  }, [loadZones]);

  // Poll the latest snapshot for the visible area
  useEffect(() => {
    if (!bounds) return;
    let cancelled = false;

    const load = async () => {
      setLoading(true);
      try {
        const response = await getLiveFlightsApi(bounds);
        if (cancelled) return;
        setFlights(response.flights);
        setSnapshotTime(response.recordedAtUtc);
        setError(null);
      } catch (err) {
        if (!cancelled) setError(getErrorMessage(err));
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void load();
    const timer = setInterval(load, REFRESH_MS);
    return () => {
      cancelled = true;
      clearInterval(timer);
    };
  }, [bounds]);

  const term = search.trim().toLowerCase();
  const filteredFlights = term
    ? flights.filter(f =>
        f.callsign.toLowerCase().includes(term) ||
        f.icao24.toLowerCase().includes(term) ||
        f.originCountry.toLowerCase().includes(term)
      )
    : flights;

  const handleCreateZoneAround = async (flight: FlightState) => {
    if (!user) return;
    try {
      await createZoneApi(user.id, {
        name: `Around ${flight.callsign || flight.icao24}`,
        priority: 1,
        areaType: 'RADIUS',
        centerLatitude: flight.latitude,
        centerLongitude: flight.longitude,
        radiusMeters: 20000,
      });
      await loadZones();
      setSelectedFlight(null);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const tiles = tileLayerFor(mapTheme);

  return (
    <div className="relative h-full w-full flex flex-col bg-[#050811] overflow-hidden">
      <div className="absolute top-4 right-4 left-4 z-[1000] flex items-center justify-between pointer-events-none">
        <div className="pointer-events-auto flex items-center space-x-3 space-x-reverse w-full max-w-md">
          <div className="relative w-full">
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('searchPlaceholder')}
              className="w-full rounded-xl border border-cyan-500/30 bg-[#0B0F19]/90 px-4 py-2.5 pl-10 text-xs text-white placeholder-gray-500 backdrop-blur-xl focus:border-cyan-400 focus:outline-none shadow-xl"
            />
            <Search size={16} className="absolute left-3 top-3 text-cyan-400" />
          </div>
        </div>

        <div className="pointer-events-auto hidden sm:flex items-center space-x-2 space-x-reverse rounded-xl border border-cyan-500/30 bg-[#0B0F19]/90 px-3 py-2 text-xs font-mono text-cyan-300 backdrop-blur-xl shadow-xl">
          <RefreshCw size={14} className={`text-cyan-400 ${loading ? 'animate-spin' : ''}`} />
          <span>
            LIVE · {filteredFlights.length} ✈
            {snapshotTime ? ` · ${new Date(snapshotTime).toLocaleTimeString()}` : ' · ממתין לנתוני OpenSky'}
          </span>
        </div>
      </div>

      {error && (
        <div className="absolute bottom-4 right-4 z-[1000] flex items-center gap-2 rounded-xl border border-rose-500/40 bg-[#0B0F19]/95 px-3 py-2 text-xs text-rose-300">
          <AlertTriangle size={14} />
          <span>{error}</span>
        </div>
      )}

      <div className="flex-1 w-full h-full z-10">
        <MapContainer center={[32.0094, 34.8854]} zoom={7} zoomControl={false} preferCanvas className="w-full h-full">
          <TileLayer key={mapTheme} url={tiles.url} attribution={tiles.attribution} className={tiles.className} />
          <BoundsWatcher onChange={setBounds} />

          <ZoneShapes zones={zones} />

          {filteredFlights.map((flight) => (
            <CircleMarker
              key={flight.icao24}
              center={[flight.latitude, flight.longitude]}
              radius={selectedFlight?.icao24 === flight.icao24 ? 8 : 4}
              pathOptions={{
                color: flight.onGround ? '#6B7280' : '#00F0FF',
                fillColor: flight.onGround ? '#6B7280' : '#00F0FF',
                fillOpacity: 0.85,
                weight: 1
              }}
              eventHandlers={{
                click: () => setSelectedFlight(flight)
              }}
            >
              <Popup>
                <div className="p-1 text-right font-sans">
                  <div className="font-mono font-bold text-cyan-600 text-sm">{flight.callsign || flight.icao24}</div>
                  <div className="text-xs text-gray-600">{flight.originCountry}</div>
                  <div className="text-[11px] text-gray-500 mt-1 font-mono">
                    Alt: {flight.baroAltitude != null ? Math.round(flight.baroAltitude) : '-'}m | Spd:{' '}
                    {flight.velocity != null ? Math.round(flight.velocity * 1.944) : '-'}kts
                  </div>
                </div>
              </Popup>
            </CircleMarker>
          ))}
        </MapContainer>
      </div>

      <FlightDrawer
        flight={selectedFlight}
        onClose={() => setSelectedFlight(null)}
        onCreateZoneAround={handleCreateZoneAround}
      />
    </div>
  );
};
