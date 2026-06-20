import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { DashboardStats, TrendItem } from '../models/Report.model';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private apiUrl = `${environment.apiUrl}/Report`;

  constructor(private http: HttpClient) {}

  getDashboard(categoryId?: string, zone?: string, from?: string, to?: string): Observable<DashboardStats> {
    let params = new HttpParams();
    if (categoryId) params = params.set('categoryId', categoryId);
    if (zone) params = params.set('zone', zone);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<DashboardStats>(`${this.apiUrl}/dashboard`, { params });
  }

  getTrend(period: 'week' | 'month' = 'week'): Observable<TrendItem[]> {
    return this.http.get<TrendItem[]>(`${this.apiUrl}/trend?period=${period}`);
  }
}
