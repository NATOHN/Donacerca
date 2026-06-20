import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';

@Injectable({ providedIn: 'root' })
export class UploadService {
  private apiUrl = `${environment.apiUrl}/Upload`;

  constructor(private http: HttpClient) {}

  uploadPhoto(file: File): Observable<{ url: string; analysis: any }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string; analysis: any }>(`${this.apiUrl}/photo`, formData);
  }
}
