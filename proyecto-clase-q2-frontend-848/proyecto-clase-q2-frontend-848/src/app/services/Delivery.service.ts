import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { DeliveryRecord, CreateDeliveryRequest } from '../models/Delivery.model';

@Injectable({ providedIn: 'root' })
export class DeliveryService {
  private apiUrl = `${environment.apiUrl}/Delivery`;

  constructor(private http: HttpClient) {}

  create(data: CreateDeliveryRequest): Observable<DeliveryRecord> {
    return this.http.post<DeliveryRecord>(this.apiUrl, data);
  }

  getByPost(postId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/post/${postId}`);
  }

  confirmDonor(postId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${postId}/confirm-donor`, {});
  }

  confirmReceiver(postId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${postId}/confirm-receiver`, {});
  }

  getHistory(): Observable<DeliveryRecord[]> {
    return this.http.get<DeliveryRecord[]>(`${this.apiUrl}/history`);
  }
}
