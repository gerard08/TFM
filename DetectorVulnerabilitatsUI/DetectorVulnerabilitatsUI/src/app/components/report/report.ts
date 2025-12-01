import { Component, model, OnInit } from '@angular/core';
import { ScanResponse } from '../../models/scanResponse';
import { ScanResults } from '../../models/db_models/scanresults';
import { Findings } from '../../models/db_models/findings';

@Component({
  selector: 'app-report',
  standalone: false,
  templateUrl: './report.html',
  styleUrl: './report.css',
})
export class Report {

  isReportShown = model.required<boolean>();
  selectedReport = model.required<ScanResponse>();
  public reportResults = model.required<ScanResults>();
  public selectedFinding: Findings = null!;

   getHistoryStatusClass(status: string): string {
    if (status === 'Net') return 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20';
    if (status === 'Crític' || status === 'Alt Risc') return 'bg-red-500 text-white shadow-lg shadow-red-500/20';
    if (status === 'Advertència') return 'bg-yellow-500 text-black';
    if (status === 'En curs') return 'bg-blue-500/10 text-blue-400 border border-blue-500/20 animate-pulse';
    return 'bg-slate-700 text-slate-300';
  }

  closeReport() {
      this.selectedReport.set(null!);
      this.isReportShown.set(false);
  }

  selectFinding(finding: Findings) {
      this.selectedFinding = finding;
  }

    getSeverityClass(severity: string): string {
      switch(severity) {
          case 'Critical': return 'bg-red-600 text-white';
          case 'High': return 'bg-orange-500 text-white';
          case 'Medium': return 'bg-yellow-500 text-black';
          case 'Low': return 'bg-blue-500 text-white';
          default: return 'bg-slate-600 text-white';
      }
  }

    getSeverityDotClass(severity: string): string {
      switch(severity) {
          case 'Critical': return 'bg-red-500';
          case 'High': return 'bg-orange-500';
          case 'Medium': return 'bg-yellow-500';
          default: return 'bg-blue-500';
      }
  }
}
