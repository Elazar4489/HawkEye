// Shapes returned by FlightTracking.Api (/api) and FlightDashboardApi (/dashboard/api).
// ASP.NET serializes to camelCase.

export type UserRole = 'Admin' | 'Operator';

export interface User {
  id: number;
  userName: string;
  email: string;
  role: UserRole;
  fone?: string | null;
  hasPushToken?: boolean;
}

export interface AuthResponse {
  user: User;
  token: string;
  expiresAtUtc: string;
}

export interface FlightState {
  icao24: string;
  callsign: string;
  originCountry: string;
  latitude: number;
  longitude: number;
  baroAltitude: number | null; // meters
  velocity: number | null; // m/s
  trueTrack: number | null; // degrees
  verticalRate: number | null; // m/s
  onGround: boolean;
  timePosition: number | null;
  lastContact: number | null;
}

export interface LiveFlightsResponse {
  recordedAtUtc: string | null;
  totalInSnapshot: number;
  flights: FlightState[];
}

export interface MapBounds {
  minLat: number;
  maxLat: number;
  minLon: number;
  maxLon: number;
}

export interface Coordinate {
  latitude: number;
  longitude: number;
}

export type AreaType = 'RADIUS' | 'POLYGON';
export type Severity = 'High' | 'Medium' | 'Low';

// Priority on the server: 1 = High, 2 = Medium, 3 = Low
export interface PriorityZone {
  id: number;
  userId: number;
  name: string | null;
  priority: number;
  areaType: AreaType;
  radiusMeters?: number | null;
  centerLatitude?: number | null;
  centerLongitude?: number | null;
  polygonCoordinates?: Coordinate[] | null;
}

export interface CreateZoneRequest {
  name: string;
  priority: number;
  areaType: AreaType;
  centerLatitude?: number;
  centerLongitude?: number;
  radiusMeters?: number;
  polygonCoordinates?: Coordinate[];
}

export interface NotificationHistoryItem {
  id: number;
  userId: number;
  message: string;
  icao24: string | null;
  callsign: string | null;
  zoneName: string | null;
  priority: number;
  severity: Severity;
  channel: string;
  latitude: number | null;
  longitude: number | null;
  baroAltitude: number | null;
  timestamp: string;
  isRead: boolean;
}

// Real-time payload of the SignalR "ReceiveFlightAlert" event
export interface FlightAlert {
  userId: number;
  message: string;
  icao24?: string | null;
  callsign?: string | null;
  flightNumber?: string | null;
  zoneName?: string | null;
  priority: number;
  latitude?: number | null;
  longitude?: number | null;
  baroAltitude?: number | null;
  matchedAtUtc: string;
}

export interface SystemHealth {
  mySqlStatus: string;
  mongoStatus: string;
  kafkaStatus: string;
  lastIngestionUtc: string | null;
  lastSnapshotFlights: number;
  activeSignalRConnections: number;
  totalUsers: number;
  totalZones: number;
}

export interface DashboardRequest {
  dashboardName: string;
  minLatitude: number;
  maxLatitude: number;
  minLongitude: number;
  maxLongitude: number;
}

export interface NamedCount {
  name: string;
  count: number;
}

export interface RiskFlight {
  icao24: string;
  callsign?: string | null;
  originCountry?: string | null;
  baroAltitude?: number | null;
  velocity?: number | null;
  verticalRate?: number | null;
  reasons: string[];
}

export interface RiskSummary {
  airborneCount: number;
  onGroundCount: number;
  lowAltitudeCount: number;
  rapidDescentCount: number;
  highSpeedCount: number;
  averageAltitude: number;
  averageVelocity: number;
  riskScore: number;
  riskLevel: Severity;
  countryBreakdown: NamedCount[];
  altitudeBands: NamedCount[];
  riskFlights: RiskFlight[];
}

export interface DashboardDocument {
  id: string;
  dashboardName: string;
  createdAt: string;
  snapshotRecordedAt: string | null;
  minLatitude: number;
  maxLatitude: number;
  minLongitude: number;
  maxLongitude: number;
  totalFlights: number;
  risk: RiskSummary;
}

export const priorityToSeverity = (priority: number): Severity =>
  priority === 1 ? 'High' : priority === 2 ? 'Medium' : 'Low';

export const severityToPriority = (severity: Severity): number =>
  severity === 'High' ? 1 : severity === 'Medium' ? 2 : 3;

export const severityColor = (severity: Severity): string =>
  severity === 'High' ? '#FF2E54' : severity === 'Medium' ? '#FF9F1C' : '#FFC700';
