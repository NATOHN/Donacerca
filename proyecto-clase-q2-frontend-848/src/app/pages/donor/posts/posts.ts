import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DonationPostService } from '../../../services/Donation-post.service';
import { AuthService } from '../../../services/auth.service';
import { DonationPost } from '../../../models/Donation-post.model';

@Component({
  selector: 'app-posts',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './posts.html',
  styleUrl: './posts.css'
})
export class PostsComponent implements OnInit {
  posts: DonationPost[] = [];
  loading = true;
  highlightId = '';

  constructor(
    private postService: DonationPostService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['highlight']) this.highlightId = params['highlight'];
    });
    this.loadPosts();
  }

  loadPosts(): void {
    this.loading = true;
    this.postService.getMine().subscribe({
      next: (posts) => {
        this.posts = posts;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.highlightId) {
          setTimeout(() => {
            const el = document.getElementById('post-' + this.highlightId);
            if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
          }, 400);
        }
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  isBlockedByAdmin(post: DonationPost): boolean {
    return !!(post.deactivatedByAdmin ?? (post as any).DeactivatedByAdmin);
  }

  deactivate(post: DonationPost): void {
    if (!confirm(`¿Desactivar "${post.itemName}"?`)) return;
    this.postService.deactivate(post.id).subscribe({
      next: () => {
        this.snackBar.open('Publicación desactivada', 'OK', { duration: 3000 });
        this.loadPosts();
      },
      error: (err) => this.snackBar.open(err.error?.message || 'Error', 'OK', { duration: 3000 })
    });
  }

  reactivate(post: DonationPost): void {
    if (!confirm(`¿Reactivar "${post.itemName}"?`)) return;
    this.postService.reactivate(post.id).subscribe({
      next: () => {
        this.snackBar.open('Publicación reactivada', 'OK', { duration: 3000 });
        this.loadPosts();
      },
      error: (err) => this.snackBar.open(err.error?.message || 'Error', 'OK', { duration: 3000 })
    });
  }

  renovate(post: DonationPost): void {
    if (!confirm(`¿Renovar "${post.itemName}"? Volverá a estar disponible.`)) return;
    this.postService.reactivate(post.id).subscribe({
      next: () => {
        this.snackBar.open('¡Publicación renovada y disponible nuevamente!', 'OK', { duration: 3000 });
        this.loadPosts();
      },
      error: (err) => this.snackBar.open(err.error?.message || 'Error', 'OK', { duration: 3000 })
    });
  }

  viewRequests(postId: string): void {
    this.router.navigate(['/donor/requests', postId]);
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => {
        this.authService.clearSession();
        this.router.navigate(['/login']);
      }
    });
  }
}
