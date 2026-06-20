import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { DonationRequestService } from '../../../services/Donation-request.service';
import { DeliveryService } from '../../../services/Delivery.service';
import { DonationPostService } from '../../../services/Donation-post.service';
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
  confirmed: { [key: string]: boolean } = {};
  highlightId = '';
  postNames: { [postId: string]: string } = {};
  postPhotos: { [postId: string]: string } = {};
  deliveryData: { [postId: string]: any } = {};

  constructor(
    private requestService: DonationRequestService,
    private deliveryService: DeliveryService,
    private postService: DonationPostService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['highlight']) {
        this.highlightId = params['highlight'];
      }
    });
    this.loadRequests();
  }

  loadRequests(): void {
    this.loading = true;
    this.requestService.getMine().subscribe({
      next: (reqs) => {
        this.requests = reqs;

        const uniquePostIds = [...new Set(reqs.map(r => r.postId))];
        const postRequests = uniquePostIds.map(postId =>
          this.postService.getById(postId).pipe(catchError(() => of(null)))
        );

        forkJoin(postRequests).subscribe({
          next: (posts: any[]) => {
            posts.forEach((post, index) => {
              const postId = uniquePostIds[index];
              if (post) {
                this.postNames[postId] = post.itemName ?? post.ItemName ?? 'Sin nombre';
                const photos = post.photoUrls ?? post.PhotoUrls ?? [];
                this.postPhotos[postId] = photos.length > 0 ? photos[0] : '';
              } else {
                this.postNames[postId] = 'Artículo eliminado';
              }
            });
            this.loading = false;
            this.cdr.detectChanges();
          },
          error: () => {
            this.loading = false;
            this.cdr.detectChanges();
          }
        });

        // Cargar datos de entrega para solicitudes aceptadas
        const accepted = reqs.filter(r => r.status === 'aceptada');
        accepted.forEach(req => {
          this.deliveryService.getByPost(req.postId).subscribe({
            next: (record: any) => {
              if (record) {
                this.deliveryData[req.postId] = record;
                if (record?.confirmedByReceiver || record?.ConfirmedByReceiver) {
                  this.confirmed[req.postId] = true;
                }
                this.cdr.detectChanges();
              }
            },
            error: () => {}
          });
        });

        if (this.highlightId) {
          setTimeout(() => {
            const el = document.getElementById('req-' + this.highlightId);
            if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
          }, 800);
        }
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  confirmReception(postId: string): void {
    if (!confirm('¿Confirmas que recibiste el artículo?')) return;
    this.confirming[postId] = true;
    this.deliveryService.confirmReceiver(postId).subscribe({
      next: () => {
        this.confirming[postId] = false;
        this.confirmed[postId] = true;
        this.cdr.detectChanges();
        this.snackBar.open('¡Recepción confirmada! Donación completada. 🎉', 'OK', { duration: 4000 });
      },
      error: (err) => {
        this.confirming[postId] = false;
        this.cdr.detectChanges();
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
