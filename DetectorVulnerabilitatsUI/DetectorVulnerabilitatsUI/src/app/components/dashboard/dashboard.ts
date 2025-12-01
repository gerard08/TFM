import { Component, computed, model, signal } from '@angular/core';
import { ScanResponse } from '../../models/scanResponse';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
activeTab = model.required<'dashboard' | 'scan' | 'results' | 'settings'>();
  scanHistory = model.required<any[]>();

  criticalScans = computed(() => this.scanHistory().filter((x) => x.state === 'CRITICAL'));
  differentAssets = computed(() => {
    const targetsOnly = this.scanHistory()
      .map((x) => x.target)
      .filter((t) => t != null && t !== '');
    return [...new Set(targetsOnly)];
  });

    getStatusClass(status: string): string {
    return status === 'Net'
      ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20'
      : 'bg-yellow-500/10 text-yellow-400 border-yellow-500/20';
  }
}
