import { ScanRequest } from './../../models/scanrequest';
import { Component, model, signal } from '@angular/core';
import { SCAN_TYPES } from '../../models/scan-types';
import { FUNNY_MESSAGES } from '../../models/missatgesDivertits';
import { ScanService } from '../../services/scan-service/scan.service';
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
