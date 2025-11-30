import { ScanRequest } from './../../models/scanrequest';
import { Component, model, signal } from '@angular/core';
import { SCAN_TYPES } from '../../models/scan-types';
import { FUNNY_MESSAGES } from '../../models/missatgesDivertits';
import { ScanService } from '../../services/scan-service/scan.service';
import { scan } from 'rxjs';
import { tipusEscaneig } from '../../models/tipusEscaneig';

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
  funnyMessages = FUNNY_MESSAGES;
  currentFunnyMessage = signal<string>(this.funnyMessages[0]);

  stats = model.required<{
    scansTotal: number;
    vulnsHigh: number;
    uniqueTargets: number;
    lastScan: string;
    status: string;
  }>();

  targetUrl = signal<string>('');
  // selectedScanType = signal<string>('Services');
  selectedScan = signal<tipusEscaneig>(SCAN_TYPES[0]);
  isScanning = signal<boolean>(false);
  scanProgress = signal<number>(0);
  elapsedTime = signal<string>('00:00');

  private scanInterval: any;
  private startTime: number = 0;
  private messageInterval: any;

  private scanService:ScanService;


  constructor()
  {
    this.scanService = new ScanService();
  }

  getScanTypeClass(typeId: number) {
    return this.selectedScan().id === typeId
      ? 'bg-emerald-500/20 border-emerald-500 text-emerald-400 ring-1 ring-emerald-500/50'
      : 'bg-slate-950 border-slate-700 text-slate-400 hover:border-slate-600 hover:bg-slate-800';
  }

  handleUrlInput(event: any) {
    this.targetUrl.set(event.target.value);
  }

  selectScanType(typeId: number) {
    this.selectedScan.set(SCAN_TYPES[typeId]);
  }

  startScan() {
    if (!this.targetUrl()) return;

    this.isScanning.set(true);

    // 2. Afegir "Pendent" a l'historial immediatament

    var scan = {
      Target: this.targetUrl(),
      ScanType: this.selectedScan().id
    } as ScanRequest;

console.log(scan);

    this.scanService.startScan(scan).subscribe({
      next: (response) => {
        console.log('Èxit! Resposta del servidor:', response);
      },
      error: (error) => {
        console.error('Error enviant scan:', error);
      }
    });
    // Iniciar rotació de missatges graciosos
    this.startFunnyMessages();

    // Simulació de finalització (Només per demo)
    // setTimeout(() => {
    //   this.finishScanInBackground(newScanId);
    // }, 12000);
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

  finishScanInBackground(scanId: number) {
    // Simula el backend acabant
    this.scanHistory.update((history) => {
      return history.map((item) => {
        if (item.id === scanId) {
          const isClean = Math.random() > 0.5;
          return {
            ...item,
            status: isClean ? 'Net' : 'Advertència',
            findings: isClean ? 0 : Math.floor(Math.random() * 5) + 1,
            duration: '14m 20s',
          };
        }
        return item;
      });
    });
  }

  stopScan() {
    if (this.scanInterval) clearInterval(this.scanInterval);
    this.isScanning.set(false);
    this.scanLogs.set([]);
    this.elapsedTime.set('00:00');
  }

  resetScan() {
    this.isScanning.set(false);
    this.targetUrl.set('');
    if (this.messageInterval) clearInterval(this.messageInterval);
  }

  startFunnyMessages() {
    // Canviar missatge cada 3 segons
    this.currentFunnyMessage.set(this.funnyMessages[0]);
    this.messageInterval = setInterval(() => {
      const idx = Math.floor(Math.random() * this.funnyMessages.length);
      this.currentFunnyMessage.set(this.funnyMessages[idx]);
    }, 3000);
  }
}
