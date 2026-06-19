import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { DonationRequestService } from '../../../services/Donation-request.service';
import { DeliveryService } from '../../../services/Delivery.service';
import { AuthService } from '../../../services/auth.service';
import { DonationRequest } from '../../../models/Donation-request.model';

@Component({
  selector: 'app-my-requests',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatSnackBarModule, MatStepperModule
  ],
  templateUrl: './my-requests.html',
  styleUrl: './my-requests.css'
})
export class MyRequestsComponent implements OnInit {
  requests: DonationRequest[] = [];
  loading = true;
  confirming: { [key: string]: boolean } = {};

  constructor(
    private requestService: DonationRequestService,
    private deliveryService: DeliveryService,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void { this.loadRequests(); }

  loadRequests(): void {
    this.loading = true;
    this.requestService.getMine().subscribe({
      next: (reqs) => { this.requests = reqs; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  confirmReception(postId: string): void {
    if (!confirm('¿Confirmas que recibiste el artículo?')) return;
    this.confirming[postId] = true;
    this.deliveryService.confirmReceiver(postId).subscribe({
      next: () => {
        this.confirming[postId] = false;
        this.snackBar.open('¡Recepción confirmada! Donación completada. 🎉', 'OK', { duration: 4000 });
        this.loadRequests();
      },
      error: (err) => {
        this.confirming[postId] = false;
        this.snackBar.open(err.error?.message || 'Error al confirmar', 'OK', { duration: 3000 });
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigate(['/login']));
  }

  getStatusIcon(status: string): string {
    const icons: { [key: string]: string } = {
      'pendiente': 'hourglass_empty',
      'aceptada': 'check_circle',
      'rechazada': 'cancel',
      'cancelada': 'block'
    };
    return icons[status] || 'help';
  }
}
