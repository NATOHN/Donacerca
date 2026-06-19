import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
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

  constructor(
    private postService: DonationPostService,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void { this.loadPosts(); }

  loadPosts(): void {
    this.loading = true;
    this.postService.getMine().subscribe({
      next: (posts) => { this.posts = posts; this.loading = false; },
      error: () => { this.loading = false; }
    });
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
