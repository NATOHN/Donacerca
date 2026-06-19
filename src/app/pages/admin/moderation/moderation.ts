import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DonationPostService } from '../../../services/Donation-post.service';
import { DonationPost } from '../../../models/Donation-post.model';

@Component({
  selector: 'app-moderation',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './moderation.html',
  styleUrl: './moderation.css'
})
export class ModerationComponent implements OnInit {
  posts: DonationPost[] = [];
  loading = true;

  constructor(
    private postService: DonationPostService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void { this.loadPosts(); }

  loadPosts(): void {
    this.loading = true;
    this.postService.getAll().subscribe({
      next: (posts) => { this.posts = posts; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  deactivate(post: DonationPost): void {
    if (!confirm(`¿Desactivar la publicación "${post.itemName}"?`)) return;
    this.postService.deactivate(post.id).subscribe({
      next: () => {
        this.snackBar.open('Publicación desactivada', 'OK', { duration: 3000 });
        // Marcar como inactiva sin recargar
        const index = this.posts.findIndex(p => p.id === post.id);
        if (index !== -1) {
          this.posts[index] = { ...this.posts[index], isActive: false };
        }
      },
      error: (err) => this.snackBar.open(err.error?.message || 'Error', 'OK', { duration: 3000 })
    });
  }
}
