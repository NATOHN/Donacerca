import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DonationPostService } from '../../services/Donation-post.service';
import { DonationRequestService } from '../../services/Donation-request.service';
import { AuthService } from '../../services/auth.service';
import { DonationPost } from '../../models/Donation-post.model';

@Component({
  selector: 'app-item-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './item-detail.html',
  styleUrl: './item-detail.css'
})
export class ItemDetailComponent implements OnInit {
  post: DonationPost | null = null;
  loading = true;
  requesting = false;
  selectedPhoto = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private postService: DonationPostService,
    private requestService: DonationRequestService,
    private authService: AuthService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.postService.getById(id).subscribe({
      next: (post) => { this.post = post; this.loading = false; },
      error: () => { this.loading = false; this.router.navigate(['/catalog']); }
    });
  }

  isLoggedIn(): boolean { return this.authService.isLoggedIn(); }
  isReceiver(): boolean { return this.authService.isReceiver(); }

  requestItem(): void {
    if (!this.post) return;
    this.requesting = true;
    this.requestService.create(this.post.id).subscribe({
      next: () => {
        this.requesting = false;
        this.snackBar.open('¡Solicitud enviada exitosamente!', 'OK', { duration: 3000 });
        this.router.navigate(['/receiver/my-requests']);
      },
      error: (err) => {
        this.requesting = false;
        this.snackBar.open(err.error?.message || 'Error al enviar solicitud', 'OK', { duration: 3000 });
      }
    });
  }
}
