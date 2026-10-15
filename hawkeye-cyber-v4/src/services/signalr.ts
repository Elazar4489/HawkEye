import * as signalR from '@microsoft/signalr';
import { getStoredConfig } from './api';
import { FlightAlert } from '../types';

export type ConnectionStatus = 'connected' | 'connecting' | 'disconnected';

// Real-time alerts pushed by FlightTracking.Api (NotificationHub -> "ReceiveFlightAlert").
class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private retryTimer: ReturnType<typeof setTimeout> | null = null;
  private stopped = true;

  public async startConnection(
    token: string,
    onAlert: (alert: FlightAlert) => void,
    onStatus: (status: ConnectionStatus) => void
  ) {
    this.stopConnection();
    this.stopped = false;

    const url = getStoredConfig().signalRHubUrl;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();
    this.connection = connection;

    // Events of a replaced connection are ignored.
    const isCurrent = () => this.connection === connection;

    connection.on('ReceiveFlightAlert', (alert: FlightAlert) => onAlert(alert));
    connection.onreconnecting(() => isCurrent() && onStatus('connecting'));
    connection.onreconnected(() => isCurrent() && onStatus('connected'));
    connection.onclose(() => {
      if (!isCurrent()) return;
      onStatus('disconnected');
      this.scheduleRetry(token, onAlert, onStatus);
    });

    onStatus('connecting');
    try {
      await connection.start();
      if (!isCurrent()) return;
      onStatus('connected');
      console.log('SignalR Hub Connected successfully to', url);
    } catch (err) {
      if (!isCurrent()) return;
      console.warn('SignalR connection failed, retrying in 5s:', err);
      onStatus('disconnected');
      this.scheduleRetry(token, onAlert, onStatus);
    }
  }

  // withAutomaticReconnect does not cover the very first start or a final close.
  private scheduleRetry(
    token: string,
    onAlert: (alert: FlightAlert) => void,
    onStatus: (status: ConnectionStatus) => void
  ) {
    if (this.stopped || this.retryTimer) return;
    this.retryTimer = setTimeout(() => {
      this.retryTimer = null;
      if (!this.stopped) {
        void this.startConnection(token, onAlert, onStatus);
      }
    }, 5000);
  }

  public stopConnection() {
    this.stopped = true;
    if (this.retryTimer) {
      clearTimeout(this.retryTimer);
      this.retryTimer = null;
    }
    if (this.connection) {
      const connection = this.connection;
      this.connection = null;
      void connection.stop();
    }
  }
}

export const signalRService = new SignalRService();
