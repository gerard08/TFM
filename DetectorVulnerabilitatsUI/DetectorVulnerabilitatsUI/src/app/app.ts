import { Component, signal, effect, ViewChild, ElementRef, WritableSignal } from '@angular/core';
import { INITIAL_STATS, INITIAL_LOGS, INITIAL_HISTORY } from './helpers/mockData';
import { ScanCompletedEvent, SignalrService } from './services/signalr.service';
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { Results } from './components/results/results';

@Component({
  selector: 'app-root',
  standalone: false,
  templateUrl: './app.html',
})
export class App {
  // Signals
  activeTab = signal<'dashboard' | 'scan' | 'results' | 'settings'>('dashboard');
  scanLogs = signal<any[]>([]);

  // --- ESTAT PERSISTENT (Signals) ---
  // Aquests senyals es carreguen de localStorage si existeix, sino usen els valors inicials
  stats = signal(this.loadData('vuln_stats', INITIAL_STATS));
  logs = signal(this.loadData('vuln_logs', INITIAL_LOGS));
  scanHistory = signal(this.loadData('vuln_history', INITIAL_HISTORY));

  private signalRSubscription!: Subscription;

  @ViewChild('logsContainer') logsContainer!: ElementRef;

  constructor(private signalrService: SignalrService, private toastr: ToastrService) {
    // 1. Auto-scroll logs
    effect(() => {
      const logs = this.scanLogs();
      setTimeout(() => {
        if (this.logsContainer) {
          this.logsContainer.nativeElement.scrollTop =
            this.logsContainer.nativeElement.scrollHeight;
        }
      }, 0);
    });

    // 2. Persistència automàtica (Effect)
    // Cada cop que canviï stats, logs o scanHistory, es guardarà al localStorage
    effect(() => {
      localStorage.setItem('vuln_stats', JSON.stringify(this.stats()));
      localStorage.setItem('vuln_logs', JSON.stringify(this.logs()));
      localStorage.setItem('vuln_history', JSON.stringify(this.scanHistory()));
    });

    this.loadScans();
    this.signalRSubscription = this.signalrService.scanFinished$.subscribe(
      (event: ScanCompletedEvent) => {
        this.toastr.success(`REBUT AMB SUMMARY ${event.Summary}`);
        this.loadScans();
      }
    );
  }

  loadScans()
  {

  }

  // --- Mètodes de Persistència ---
  loadData(key: string, fallback: any): any {
    const stored = localStorage.getItem(key);
    return stored ? JSON.parse(stored) : fallback;
  }

  clearStorage() {
    localStorage.removeItem('vuln_stats');
    localStorage.removeItem('vuln_logs');
    localStorage.removeItem('vuln_history');
    // Reset a valors inicials
    this.stats.set(INITIAL_STATS);
    this.logs.set(INITIAL_LOGS);
    this.scanHistory.set(INITIAL_HISTORY);
    alert('Dades esborrades correctament.');
  }

  // Helpers
  getTabClass(tabName: string) {
    return this.activeTab() === tabName
      ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 shadow-lg shadow-emerald-500/5'
      : 'text-slate-400 hover:bg-slate-800 hover:text-white';
  }

  getStatusClass(status: string): string {
    return status === 'Net'
      ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20'
      : 'bg-yellow-500/10 text-yellow-400 border-yellow-500/20';
  }

  getLogClass(log: any): string {
    return 'text-slate-300';
  }

  getLogTime(): string {
    return new Date().toLocaleTimeString();
  }
}
