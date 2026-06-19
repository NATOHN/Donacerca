import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DonationPostService } from '../../services/Donation-post.service';
import { CategoryService } from '../../services/Category.service';
import { ZoneService } from '../../services/Zone.service';
import { DonationPost } from '../../models/Donation-post.model';
import { Category } from '../../models/Category.model';
import { Zone } from '../../models/Zone.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [
    CommonModule, RouterLink, FormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatSelectModule, MatInputModule,
    MatChipsModule, MatProgressSpinnerModule
  ],
  templateUrl: './catalog.html',
  styleUrl: './catalog.css'
})
export class CatalogComponent implements OnInit {
  posts: DonationPost[] = [];
  categories: Category[] = [];
  zones: Zone[] = [];
  loading = true;
  selectedCategory = '';
  selectedZone = '';

  constructor(
    private postService: DonationPostService,
    private categoryService: CategoryService,
    private zoneService: ZoneService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.categoryService.getAll().subscribe((c: Category[]) => this.categories = c);
    this.zoneService.getAll().subscribe((z: Zone[]) => this.zones = z);
    this.loadPosts();
  }

  loadPosts(): void {
    this.loading = true;
    this.postService.getAvailable(
      this.selectedCategory || undefined,
      this.selectedZone || undefined
    ).subscribe({
      next: (posts) => { this.posts = posts; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  clearFilters(): void {
    this.selectedCategory = '';
    this.selectedZone = '';
    this.loadPosts();
  }

  getConditionColor(condition: string): string {
    switch(condition) {
      case 'nuevo': return 'accent';
      case 'buen estado': return 'primary';
      default: return '';
    }
  }

  viewDetail(id: string): void {
    this.router.navigate(['/catalog', id]);
  }
}
