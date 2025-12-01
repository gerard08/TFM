import { ScanResponse } from './models/scanResponse';
import {
  Component,
  signal,
  effect,
  ViewChild,
  ElementRef,
  computed,
} from '@angular/core';
import { ScanCompletedEvent, SignalrService } from './services/signalr.service';
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { ScanService } from './services/scan-service/scan.service';

@Component({
  selector: 'app-root',
  standalone: false,
  templateUrl: './app.html',
})
export class App {
  activeTab = signal<'dashboard' | 'scan' | 'results' | 'settings'>('dashboard');
  scanHistory = signal<ScanResponse[]>([]);
  private signalRSubscription!: Subscription;

  @ViewChild('logsContainer') logsContainer!: ElementRef;

  constructor(
    private signalrService: SignalrService,
    private toastr: ToastrService,
    private scanService: ScanService
  ) {
    this.loadScans();
    this.signalRSubscription = this.signalrService.scanFinished$.subscribe(
      (event: ScanCompletedEvent) => {
        this.toastr.success(`Resultat de l'escaneig rebut, el pots veure a la pestanya de resultats`);
        this.loadScans();
      }
    );
  }

  loadScans() {
    this.scanService.getResults().subscribe({
      next: (response) => {
        console.log('Èxit! Resposta del servidor:', response);
        this.scanHistory.set(response);
      },
      error: (error) => {
        console.error('Error enviant scan:', error);
      },
    });
  }

  loadData(key: string, fallback: any): any {
    const stored = localStorage.getItem(key);
    return stored ? JSON.parse(stored) : fallback;
  }

  clearStorage() {
    localStorage.removeItem('vuln_stats');
    localStorage.removeItem('vuln_logs');
    localStorage.removeItem('vuln_history');
    this.scanHistory.set(null!);
    alert('Dades esborrades correctament.');
  }

  getTabClass(tabName: string) {
    return this.activeTab() === tabName
      ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 shadow-lg shadow-emerald-500/5'
      : 'text-slate-400 hover:bg-slate-800 hover:text-white';
  }
}
