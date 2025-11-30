import { Component, model, OnDestroy, OnInit } from '@angular/core';
import { ScanService } from '../../services/scan-service/scan.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-results',
  standalone: false,
  templateUrl: './results.html',
  styleUrl: './results.css',
})
export class Results implements OnInit, OnDestroy {
  private signalRSubscription!: Subscription;
  scanHistory = model.required<any[]>();

  constructor(private scanService: ScanService) {}

  ngOnInit(): void {
    this.loadScans;
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

  ngOnDestroy() {
    // Molt important: Desubscriure's per evitar memory leaks
    if (this.signalRSubscription) {
      this.signalRSubscription.unsubscribe();
    }
  }

  getHistoryStatusClass(status: string): string {
    if (status === 'SAFE') {
      return 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20';
    } else if (status === 'CRITICAL') {
      return 'bg-red-500 text-white shadow-lg shadow-red-500/20';
    } else if (status === 'WARNING') {
      return 'bg-yellow-500 text-black';
    } else if (status === 'RUNNING') {
      return 'bg-blue-500 text-black';
    } else {
      return 'bg-slate-700 text-slate-300';
    }
  }
}
