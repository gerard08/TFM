import { ScanRequest } from './../../models/scanrequest';
import { Component, model, signal } from '@angular/core';
import { SCAN_TYPES } from '../../models/scan-types';
import { SCAN_MESSAGES } from '../../models/missatgesDivertits';
import { ScanService } from '../../services/scan-service/scan.service';
import { tipusEscaneig } from '../../models/tipusEscaneig';
import { ToastrService } from 'ngx-toastr';

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

  funnyMessages = SCAN_MESSAGES;

  currentFunnyMessage = signal<string>(this.funnyMessages[0]);
  targetUrl = signal<string>('');
  selectedScan = signal<tipusEscaneig>(SCAN_TYPES[0]);
  isScanning = signal<boolean>(false);
  scanProgress = signal<number>(0);
  elapsedTime = signal<string>('00:00');

  private messageInterval: any;

  constructor(private toastr: ToastrService, private scanService: ScanService) {}

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
      ScanType: this.selectedScan().id,
    } as ScanRequest;

    this.scanService.startScan(scan).subscribe({
      next: (response) => {
        console.log('Èxit! Resposta del servidor:', response);
        this.toastr.success(`Escaneig enviat al servidor correctament`);
      },
      error: (error) => {
        console.error('Error enviant scan:', error);
        this.toastr.error(`Error en processar l'escaneig`);
      },
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
