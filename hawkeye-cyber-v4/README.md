# HawkEye - Cyber Real-Time Flight Tracking & Alerts System Frontend (v4.0)

This is a complete, production-grade **React + TypeScript + Vite + Tailwind CSS** frontend project for **HawkEye**.

## Features Included:
1. **Full Step-by-Step UI Flow**:
   - **Splash Screen (`SplashScreen.tsx`)**: Futuristic landing with animated radar scanner & CTA.
   - **Authentication (`AuthPage.tsx`)**: Login/Register, MySQL Users DB check, and Browser Push Notification permission prompt (FCM). Access to system is strictly guarded.
   - **Live Tracking Map (`LiveMapPage.tsx`)**: Leaflet map supporting **Dark Cyber Vector Map** and **Light Map** with live toggle, aircraft markers, and telemetry drawer (`FlightDrawer.tsx`).
   - **Priority Spatial Zones (`PriorityZonesPage.tsx`)**: Radius & Polygon setup with radius sliders and MySQL Spatial SQL integration.
   - **Alerts Center (`AlertsPage.tsx`)**: Real-time Toast alerts + searchable boundary crossing history.
   - **Analytics & Reports (`AnalyticsPage.tsx`)**: Recharts graphs, KPI cards, and MongoDB state query interface.
   - **Admin Console (`AdminPage.tsx`)**: User management & live infrastructure health monitor (Kafka, MySQL, MongoDB, SignalR).

2. **Dual HTTP/HTTPS Protocol Support**:
   - Out-of-the-box HTTPS server via `@vitejs/plugin-basic-ssl` (`https://localhost:3000`).
   - Backend Config Modal (`BackendConfigModal.tsx`) for switching between HTTPS (`https://localhost:5001`) and HTTP (`http://localhost:5000`).

3. **Dual Language & Theme Support**:
   - Instant language switch button (Hebrew RTL / English LTR).
   - Instant map theme toggle (Dark Cyber / Light Map).

## How to Run:
```bash
npm install
npm run dev
```
Then open `https://localhost:3000` or `http://localhost:3000`.
