import { Component, model } from '@angular/core';

@Component({
  selector: 'app-results',
  standalone: false,
  templateUrl: './results.html',
  styleUrl: './results.css',
})
export class Results {
  scanHistory = model.required<any[]>();

    getHistoryStatusClass(status: string): string {
    if (status === 'Net') {
      return 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20';
    } else if (status === 'Crític' || status === 'Alt Risc') {
      return 'bg-red-500 text-white shadow-lg shadow-red-500/20';
    } else if (status === 'Advertència') {
      return 'bg-yellow-500 text-black';
    } else {
      return 'bg-slate-700 text-slate-300';
    }
  }
}
