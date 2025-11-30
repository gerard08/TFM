import { Component, model, OnDestroy, OnInit } from '@angular/core';
import { ScanService } from '../../services/scan-service/scan.service';
import { Subscription } from 'rxjs';
import { ScanCompletedEvent, SignalrService } from '../../services/signalr.service';
import {ToastrService} from "ngx-toastr"
@Component({
  selector: 'app-results',
  standalone: false,
  templateUrl: './results.html',
  styleUrl: './results.css',
})
export class Results implements OnDestroy {

  private signalRSubscription!: Subscription;
  scanHistory = model.required<any[]>();

  constructor(
  )
  {}

  ngOnDestroy() {
    // Molt important: Desubscriure's per evitar memory leaks
    if (this.signalRSubscription) {
      this.signalRSubscription.unsubscribe();
    }
  }

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
