import React from 'react';
import { Circle, Polygon, Tooltip } from 'react-leaflet';
import { PriorityZone, priorityToSeverity, severityColor } from '../types';

// Draws saved priority zones (radius = circle, polygon = polygon) on a Leaflet map.
export const ZoneShapes: React.FC<{ zones: PriorityZone[] }> = ({ zones }) => (
  <>
    {zones.map((zone) => {
      const color = severityColor(priorityToSeverity(zone.priority));
      const pathOptions = { color, fillColor: color, fillOpacity: 0.12, weight: 2 };
      const label = zone.name || `Zone #${zone.id}`;

      if (zone.areaType === 'RADIUS' && zone.centerLatitude != null && zone.centerLongitude != null) {
        return (
          <Circle
            key={zone.id}
            center={[zone.centerLatitude, zone.centerLongitude]}
            radius={zone.radiusMeters || 10000}
            pathOptions={pathOptions}
          >
            <Tooltip>{label}</Tooltip>
          </Circle>
        );
      }

      if (zone.areaType === 'POLYGON' && zone.polygonCoordinates && zone.polygonCoordinates.length >= 3) {
        return (
          <Polygon
            key={zone.id}
            positions={zone.polygonCoordinates.map((c) => [c.latitude, c.longitude] as [number, number])}
            pathOptions={pathOptions}
          >
            <Tooltip>{label}</Tooltip>
          </Polygon>
        );
      }

      return null;
    })}
  </>
);

// OpenStreetMap needs no API key; the dark theme is the same tiles through a CSS filter
// (.map-tiles-dark in index.css).
export const tileLayerFor = (mapTheme: 'dark' | 'light') => ({
  url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
  attribution: '&copy; OpenStreetMap contributors',
  className: mapTheme === 'dark' ? 'map-tiles-dark' : '',
});
