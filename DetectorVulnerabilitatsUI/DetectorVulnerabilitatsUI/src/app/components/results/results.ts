import { Component, computed, model, OnDestroy, OnInit, signal } from '@angular/core';
import { ScanService } from '../../services/scan-service/scan.service';
import { lastValueFrom, Subscription } from 'rxjs';
import { ScanResponse } from '../../models/scanResponse';
import { ScanResults } from '../../models/db_models/scanresults';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-results',
  standalone: false,
  templateUrl: './results.html',
  styleUrl: './results.css',
})
export class Results implements OnInit, OnDestroy {
  private signalRSubscription!: Subscription;
  scanHistory = model.required<any[]>();
  isReportShown = signal<boolean>(false);
  selectedReport = signal<ScanResponse>(null!);
  public reportResults = signal<ScanResults>(null!);
  isRefreshing = signal(false);

  searchQuery = signal<string>('');
  sortColumn = signal<string>('date'); // Default sort by date
  sortDirection = signal<'asc' | 'desc'>('desc'); // Default new to old

  constructor(private scanService: ScanService, private toastr: ToastrService) {
    this.loadScans;
  }

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

  refreshList() {
    this.isRefreshing.set(true);
    // Simulem una petició al servidor
    setTimeout(() => {
      // Aquí recarregariem les dades reals
      this.loadScans();
      this.isRefreshing.set(false);
    }, 1000);
  }

  viewReport(scan: ScanResponse) {
    this.selectedReport.set(scan);
    console.log(`selected report is ${this.selectedReport()}`);

    this.scanService.getResult(this.selectedReport().id).subscribe({
      next: (response) => {
        console.log('Èxit! Resposta del servidor:', response);
        this.reportResults.set(response);
        this.isReportShown.set(true);
      },
      error: (error) => {
        console.error('Error enviant scan:', error);
      },
    });
  }

  getReport(scan: ScanResponse) {
    return this.scanService.getResult(scan.id);
  }

  filteredHistory = computed(() => {
    let data = [...this.scanHistory()];
    const query = this.searchQuery().toLowerCase();
    const col = this.sortColumn();
    const dir = this.sortDirection();

    // Filter
    if (query) {
      data = data.filter((item) => item.target.target.toLowerCase().includes(query));
    }

    // Sort
    if (col) {
      data.sort((a: any, b: any) => {
        let aVal = a[col];
        let bVal = b[col];

        // Simple string/number comparison
        if (aVal < bVal) return dir === 'asc' ? -1 : 1;
        if (aVal > bVal) return dir === 'asc' ? 1 : -1;
        return 0;
      });
    }

    return data;
  });

  updateSearch(e: any) {
    this.searchQuery.set(e.target.value);
  }

  toggleSort(column: string) {
    if (this.sortColumn() === column) {
      this.sortDirection.update((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('asc');
    }
  }

  getSortIconClass(column: string): string {
    if (this.sortColumn() !== column) return 'opacity-0'; // Hide if not active
    return this.sortDirection() === 'asc' ? 'rotate-180 opacity-100' : 'opacity-100';
  }

 async exportToCsv() {
  const scanHistory = this.scanHistory();

  if (!scanHistory || scanHistory.length === 0) {
    this.toastr.error('No hi ha dades per exportar');
    return;
  }

  const headers = [
    'Scan ID', 'Objectiu', 'Data Escaneig', 'Estat', 'Durada',
    'Finding ID', 'Severitat', 'Títol Vulnerabilitat', 'CVE',
    'Servei Afectat', 'Descripció', 'Solució'
  ];

  try {
    const rowPromises = scanHistory.map(async (scan) => {

      if (scan.vulnerabilityCount == 0 || scan.state !== 'FINISHED' && scan.state !== 'CRITICAL' && scan.state !== 'WARNING' && scan.state !== 'SAFE') {
         return [[
            scan.id,
            scan.target,
            scan.date,
            scan.state,
            scan.duration,
            '', '', '', '', '', '', ''
         ]];
      }

      // CAS B: Si té resultats, els hem de descarregar
      try {
        // Fes servir lastValueFrom per convertir l'Observable en una Promesa i esperar
        // ALERTA: Cridem directament al servei per evitar l'error de tipus del 'getReport'
        const report = await lastValueFrom(this.scanService.getResult(scan.id));

        if (!report || !report.findings || report.findings.length === 0) {
           return [[
            scan.id, scan.target, scan.date, scan.state, scan.duration,
            '', '', '', '', '', '', ''
           ]];
        }

        // Retornem una fila per cada finding trobat
        return report.findings.map(finding => [
            scan.id,
            scan.target,
            scan.date,
            scan.state,
            scan.duration,
            finding.id,
            finding.severity,
            finding.title,
            finding.cve_id,
            finding.affected_service,
            finding.description, // Alerta: Si hi ha salts de línia pot trencar el CSV
            finding.solution
        ]);

      } catch (err) {
        console.error(`Error obtenint detalls per scan ${scan.id}`, err);
        // En cas d'error, retornem la fila bàsica amb un avís
        return [[
            scan.id, scan.target, scan.date, "ERROR_FETCHING", scan.duration,
            '', '', '', '', '', '', ''
        ]];
      }
    });

    // 2. Esperem que TOTES les peticions al servidor acabin (Parallel requests)
    const rowsArrays = await Promise.all(rowPromises);

    // 3. Aplanem l'array (perquè tenim un array d'arrays de files)
    const allRows = rowsArrays.flat();

    // 4. Generem el contingut CSV
    const csvContent = [
      headers.join(','),
      ...allRows.map(row => row.map(field => {
        // Sanitització del CSV: Escapar cometes i posar cometes dobles
        const str = String(field || '').replace(/"/g, '""');
        return `"${str}"`;
      }).join(','))
    ].join('\n');

    // 5. Descarreguem l'arxiu
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `vulnguard_report_${new Date().getTime()}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

  } catch (e) {
    console.error('Error global generant CSV', e);
    this.toastr.error('Error generant el fitxer CSV');
  }
}
}
