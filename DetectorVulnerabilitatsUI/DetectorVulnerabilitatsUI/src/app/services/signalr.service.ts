import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

// Interfície del missatge que envia l'API
export interface ScanCompletedEvent {
  Id: string; // Guid ve com string a JS
  Scan_task_id: string;
  Summary: string;
  Created_at: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection;

  // Utilitzem un Subject de RxJS per fer-ho "Angular-friendly"
  private scanFinishedSubject = new Subject<ScanCompletedEvent>();
  public scanFinished$ = this.scanFinishedSubject.asObservable();

  constructor() {
    // 1. Configurem la connexió
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:8080/scanhub') // La URL del teu backend + /scanhub
      .withAutomaticReconnect() // Si cau la xarxa, reintenta sol
      .build();

    // 2. Iniciem la connexió
    this.startConnection();

    // 3. Escoltem l'event que ve del Backend ("ScanFinished")
    this.hubConnection.on('ScanFinished', (data: ScanCompletedEvent) => {
      console.log('SignalR Event Rebut:', data);

      // Passem la informació a la resta de l'app via RxJS
      this.scanFinishedSubject.next(data);
    });
  }

  private startConnection() {
    this.hubConnection
      .start()
      .then(() => console.log('Connexió SignalR establerta!'))
      .catch(err => console.error('Error connectant SignalR:', err));
  }
}
