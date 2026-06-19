import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DonationPostService } from '../../../services/Donation-post.service';
import { AuthService } from '../../../services/auth.service';
import { DonationPost } from '../../../models/Donation-post.model';

@Component({
  selector: 'app-donor-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DonorDashboardComponent implements OnInit {
  posts: DonationPost[] = [];
  loading = true;
  userName = '';

  get activePosts() { return this.posts.filter(p => p.status === 'disponible').length; }
  get reservedPosts() { return this.posts.filter(p => p.status === 'reservado').length; }
  get completedPosts() { return this.posts.filter(p => p.status === 'entregado').length; }

  constructor(
    private postService: DonationPostService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getCurrentUser()?.fullName || 'Donante';
    this.postService.getMine().subscribe({
      next: (posts) => { this.posts = posts; this.loading = false; },
      error: () => { this.loading = false; }
    });
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
