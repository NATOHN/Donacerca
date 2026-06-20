import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { Zone } from '../models/Zone.model';

@Injectable({ providedIn: 'root' })
export class ZoneService {
  private apiUrl = `${environment.apiUrl}/Zone`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Zone[]> {
    return this.http.get<Zone[]>(this.apiUrl);
  }
}
