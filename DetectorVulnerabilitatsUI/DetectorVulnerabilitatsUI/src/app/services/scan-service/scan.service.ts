import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ScanRequest } from '../../models/scanrequest';

@Injectable({
  providedIn: 'root'
})
export class ScanService {
  // Injectem el client HTTP d'Angular
  private http = inject(HttpClient);

  // 2. La URL base del teu servidor (Backend)
  // Si fas servir Node/Express, normalment és localhost:3000
  // Si fas servir Python/Django/Flask, pot ser localhost:5000 o 8000
  private apiUrl = 'http://localhost:8080';

  // --- LECTURA DE DADES (GET) ---

  // Obtenir tot l'historial
  // getHistory(): Observable<ScanLog[]> {
  //   return this.http.get<ScanLog[]>(`${this.apiUrl}/scans`);
  // }

  // --- ACCIONS (POST/DELETE) ---

  // Iniciar un nou escaneig
  startScan(scanRequest: ScanRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/requestscan`, scanRequest);
  }

  // // Aturar un escaneig en curs
  // stopScan(): Observable<any> {
  //   return this.http.post(`${this.apiUrl}/scan/stop`, {});
  // }

  // // Esborrar un element concret per ID
  // deleteScan(id: number): Observable<void> {
  //   return this.http.delete<void>(`${this.apiUrl}/scans/${id}`);
  // }

  // // Esborrar tot l'historial
  // clearAllHistory(): Observable<void> {
  //   return this.http.delete<void>(`${this.apiUrl}/scans/all`);
  // }
}
