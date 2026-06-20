import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin } from 'rxjs';
import { DonationPostService } from '../../services/Donation-post.service';
import { CategoryService } from '../../services/Category.service';
import { ZoneService } from '../../services/Zone.service';
import { DonationPost } from '../../models/Donation-post.model';
import { Zone } from '../../models/Zone.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive, FormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatSelectModule, MatInputModule,
    MatChipsModule, MatProgressSpinnerModule
  ],
  templateUrl: './catalog.html',
  styleUrl: './catalog.css'
})
export class CatalogComponent implements OnInit {
  posts: DonationPost[] = [];
  categories: any[] = [];
  zones: Zone[] = [];
  loading = true;
  selectedCategory = '';
  selectedZone = '';

  isLoggedIn = false;

  constructor(
    private postService: DonationPostService,
    private categoryService: CategoryService,
    private zoneService: ZoneService,
    private router: Router,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.isLoggedIn =
      !!localStorage.getItem('token') ||
      !!localStorage.getItem('authToken') ||
      !!localStorage.getItem('accessToken') ||
      !!localStorage.getItem('user');

    forkJoin({
      cats: this.categoryService.getAll(),
      zones: this.zoneService.getAll()
    }).subscribe({
      next: ({ cats, zones }: any) => {
        this.categories = cats.filter((c: any) => c.isActive ?? c.IsActive);
        this.zones = zones;
        this.cdr.detectChanges();
      }
    });

    this.loadPosts();
  }

  loadPosts(): void {
    this.loading = true;

    this.postService.getAvailable(
      this.selectedCategory || undefined,
      this.selectedZone || undefined
    ).subscribe({
      next: (posts) => {
        this.posts = posts;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  clearFilters(): void {
    this.selectedCategory = '';
    this.selectedZone = '';
    this.loadPosts();
  }

  viewDetail(id: string): void {
    this.router.navigate(['/catalog', id]);
  }
  logout(): void {
  localStorage.clear();
  this.router.navigate(['/login']);
}
}