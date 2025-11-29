import { Component, model, signal } from '@angular/core';
import { SCAN_TYPES } from '../../models/scan-types';

@Component({
  selector: 'app-scan',
  standalone: false,
  templateUrl: './scan.html',
  styleUrl: './scan.css',
})
export class Scan {
  scanTypes = SCAN_TYPES;


  activeTab = model.required<'dashboard' | 'scan' | 'results' | 'settings'>();
  scanHistory = model.required<any[]>();
  scanLogs = model.required<any[]>();
  logs = model.required<any[]>();

  stats = model.required<{
    scansTotal: number;
    vulnsHigh: number;
    uniqueTargets: number;
    lastScan: string;
    status: string;
  }>();

  targetUrl = signal<string>('');
  selectedScanType = signal<string>('Services');
  isScanning = signal<boolean>(false);
  scanProgress = signal<number>(0);
  elapsedTime = signal<string>('00:00');

  private scanInterval: any;
  private startTime: number = 0;

  getScanTypeClass(typeId: string) {
    return this.selectedScanType() === typeId
      ? 'bg-emerald-500/20 border-emerald-500 text-emerald-400 ring-1 ring-emerald-500/50'
      : 'bg-slate-950 border-slate-700 text-slate-400 hover:border-slate-600 hover:bg-slate-800';
  }

  handleUrlInput(event: any) {
    this.targetUrl.set(event.target.value);
  }

  selectScanType(typeId: string) {
    this.selectedScanType.set(typeId);
  }

  startScan() {
    if (!this.targetUrl()) return;

    this.isScanning.set(true);
    this.scanLogs.set([]);
    this.elapsedTime.set('00:00');

    this.startTime = Date.now();

    this.scanLogs.update((l) => [
      ...l,
      { msg: `Connexió establerta amb motor .NET...`, type: 'info' },
    ]);
    this.scanLogs.update((l) => [
      ...l,
      { msg: `Enviant paquet d'inici per: ${this.targetUrl()}`, type: 'info' },
    ]);

    // Timer Interval (Real-time clock + Keep-alive messages)
    this.scanInterval = setInterval(() => {
      const now = Date.now();
      const diff = now - this.startTime;

      // Update Timer Text
      const minutes = Math.floor(diff / 60000);
      const seconds = Math.floor((diff % 60000) / 1000);
      this.elapsedTime.set(
        `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
      );

      // Simulació de finalització automàtica (p.ex. al cap de 10 segons per demo)
      // En una app real, això passaria quan reps el callback del backend
      // Aquí ho faig ràpid per provar la persistència
      if (diff > 10000) {
        this.completeScanSimulation(diff);
      }

      // Heartbeats
      if (Math.floor(diff / 1000) % 3 === 0 && Math.floor(diff / 1000) > 0) {
        const heartbeats = [
          'Processant paquets...',
          'Analitzant respostes...',
          'Verificant ports...',
          'Escrivint resultats temporals...',
        ];
        const randomMsg = heartbeats[Math.floor(Math.random() * heartbeats.length)];
        this.scanLogs.update((l) => {
          const newLogs = [...l, { msg: randomMsg, type: 'dim' }];
          return newLogs.slice(-5);
        });
      }
    }, 1000);
  }

  completeScanSimulation(durationMs: number) {
    clearInterval(this.scanInterval);
    this.isScanning.set(false);

    const durationStr = `${Math.floor(durationMs / 1000)}s`;
    const today = new Date().toLocaleDateString();
    const newScanId = Math.floor(Math.random() * 10000);
    const isClean = Math.random() > 0.5; // Random result

    // 1. Crear nou objecte d'historial
    const newEntry = {
      id: newScanId,
      target: this.targetUrl(),
      date: today,
      status: isClean ? 'Net' : 'Advertència',
      findings: isClean ? 0 : Math.floor(Math.random() * 5) + 1,
      duration: durationStr,
    };

    // 2. Actualitzar els Signals (això dispararà l'effect i guardarà a localStorage)
    this.scanHistory.update((history) => [newEntry, ...history]);

    this.logs.update((logs) => [
      {
        id: newScanId,
        target: this.targetUrl(),
        date: today,
        status: isClean ? 'Net' : 'Risc',
        duration: durationStr,
      },
      ...logs,
    ]);

    // Update stats
    this.stats.update((s) => ({
      ...s,
      scansTotal: s.scansTotal + 1,
      lastScan: 'Ara mateix',
    }));

    // Canviar a la pestanya de resultats per veure el nou item
    this.activeTab.set('results');
  }

  stopScan() {
    if (this.scanInterval) clearInterval(this.scanInterval);
    this.isScanning.set(false);
    this.scanLogs.set([]);
    this.elapsedTime.set('00:00');
  }
}
