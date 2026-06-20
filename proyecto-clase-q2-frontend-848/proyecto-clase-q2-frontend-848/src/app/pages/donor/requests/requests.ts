import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DonationRequestService } from '../../../services/Donation-request.service';
import { AuthService } from '../../../services/auth.service';
import { DonationRequest } from '../../../models/Donation-request.model';

@Component({
  selector: 'app-requests',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './requests.html',
  styleUrl: './requests.css'
})
export class RequestsComponent implements OnInit {
  requests: DonationRequest[] = [];
  loading = true;
  selecting = false;
  postId = '';

  constructor(
    private requestService: DonationRequestService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.postId = this.route.snapshot.paramMap.get('postId')!;
    this.loadRequests();
  }

  loadRequests(): void {
    this.loading = true;
    this.requestService.getByPost(this.postId).subscribe({
      next: (reqs) => {
        console.log('Solicitudes recibidas:', JSON.stringify(reqs));
        this.requests = reqs;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectReceiver(receiverId: string): void {
    console.log('receiverId a seleccionar:', receiverId);
    if (!confirm('¿Seleccionar este receptor? El artículo quedará reservado para esta persona.')) return;
    this.selecting = true;
    this.requestService.selectReceiver(this.postId, receiverId).subscribe({
      next: () => {
        this.selecting = false;
        this.cdr.detectChanges();
        this.snackBar.open('¡Receptor seleccionado! Ahora coordina la entrega.', 'OK', { duration: 4000 });
        this.router.navigate(['/donor/delivery', this.postId]);
      },
      error: (err) => {
        this.selecting = false;
        this.cdr.detectChanges();
        this.snackBar.open(err.error?.message || 'Error', 'OK', { duration: 3000 });
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigate(['/login']));
  }

  getPendingRequests(): DonationRequest[] {
    return this.requests.filter(r => r.status === 'pendiente');
  }
}
