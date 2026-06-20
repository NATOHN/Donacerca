import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { DonationRequest } from '../models/Donation-request.model';

@Injectable({ providedIn: 'root' })
export class DonationRequestService {
  private apiUrl = `${environment.apiUrl}/DonationRequest`;

  constructor(private http: HttpClient) {}

  create(postId: string): Observable<DonationRequest> {
    return this.http.post<DonationRequest>(this.apiUrl, { postId });
  }

  getByPost(postId: string): Observable<DonationRequest[]> {
    return this.http.get<DonationRequest[]>(`${this.apiUrl}/post/${postId}`);
  }

  getMine(): Observable<DonationRequest[]> {
    return this.http.get<DonationRequest[]>(`${this.apiUrl}/my`);
  }

  selectReceiver(postId: string, receiverId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/select?postId=${postId}`, { receiverId });
  }
}
