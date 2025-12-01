import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ScanRequest } from '../../models/scanrequest';
import { ScanResponse } from '../../models/scanResponse';
import { ScanResults } from '../../models/db_models/scanresults';

@Injectable({
  providedIn: 'root'
})
export class ScanService {

  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:8080';

  // Iniciar un nou escaneig
  startScan(scanRequest: ScanRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/requestscan`, scanRequest);
  }

  getResults(): Observable<ScanResponse[]> {
    return this.http.get<ScanResponse[]>(`${this.apiUrl}/allscanresults`);
  }

  getResult(id: string): Observable<ScanResults> {
    let result =  this.http.get<ScanResults>(`${this.apiUrl}/scanresult/${id}`);
    console.log(result);
    return result;
  }
}
