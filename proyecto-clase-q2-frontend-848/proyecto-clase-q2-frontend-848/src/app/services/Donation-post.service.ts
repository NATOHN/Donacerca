import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { DonationPost, CreatePostRequest } from '../models/Donation-post.model';

export interface ReceiverStats {
  available: number;
  reserved:  number;
  delivered: number;
  total:     number;
  Available: number;
  Reserved:  number;
  Delivered: number;
  Total:     number;
}

@Injectable({ providedIn: 'root' })
export class DonationPostService {
  private apiUrl = `${environment.apiUrl}/DonationPost`;

  constructor(private http: HttpClient) {}

  getAvailable(categoryId?: string, zone?: string): Observable<DonationPost[]> {
    let params = new HttpParams();
    if (categoryId) params = params.set('categoryId', categoryId);
    if (zone)       params = params.set('zone', zone);
    return this.http.get<DonationPost[]>(this.apiUrl, { params });
  }

  getById(id: string): Observable<DonationPost> {
    return this.http.get<DonationPost>(`${this.apiUrl}/${id}`);
  }

  getMine(): Observable<DonationPost[]> {
    return this.http.get<DonationPost[]>(`${this.apiUrl}/my`);
  }

  getAll(): Observable<DonationPost[]> {
    return this.http.get<DonationPost[]>(`${this.apiUrl}/all`);
  }

  getForModeration(): Observable<DonationPost[]> {
    return this.http.get<DonationPost[]>(`${this.apiUrl}/moderation`);
  }

  getReceiverStats(): Observable<ReceiverStats> {
    return this.http.get<ReceiverStats>(`${this.apiUrl}/receiver-stats`);
  }

  create(data: CreatePostRequest): Observable<DonationPost> {
    return this.http.post<DonationPost>(this.apiUrl, data);
  }

  update(id: string, data: Partial<CreatePostRequest>): Observable<DonationPost> {
    return this.http.put<DonationPost>(`${this.apiUrl}/${id}`, data);
  }

  deactivate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  reactivate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/reactivate`, {});
  }
}
