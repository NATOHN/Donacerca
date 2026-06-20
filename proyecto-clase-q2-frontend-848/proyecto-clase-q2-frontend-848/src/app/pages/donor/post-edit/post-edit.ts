import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DonationPostService } from '../../../services/Donation-post.service';
import { CategoryService } from '../../../services/Category.service';
import { ZoneService } from '../../../services/Zone.service';
import { forkJoin } from 'rxjs';
import { Category } from '../../../models/Category.model';
import { Zone } from '../../../models/Zone.model';

@Component({
  selector: 'app-post-edit',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './post-edit.html',
  styleUrl: './post-edit.css'
})
export class PostEditComponent implements OnInit {
  form: FormGroup;
  categories: Category[] = [];
  zones: Zone[] = [];
  loading = true;
  saving = false;
  postId = '';

  constructor(
    private fb: FormBuilder,
    private postService: DonationPostService,
    private categoryService: CategoryService,
    private zoneService: ZoneService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      categoryId: ['', Validators.required],
      itemName: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      itemCondition: ['', Validators.required],
      zone: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.postId = this.route.snapshot.paramMap.get('id')!;
    forkJoin({
      post: this.postService.getById(this.postId),
      categories: this.categoryService.getAll(),
      zones: this.zoneService.getAll()
    }).subscribe({
      next: ({ post, categories, zones }: any) => {
        this.categories = categories.filter((c: any) => c.isActive);
        this.zones = zones;
        this.form.patchValue({
          categoryId: post.categoryId,
          itemName: post.itemName,
          description: post.description,
          itemCondition: post.itemCondition,
          zone: post.zone
        });
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.router.navigate(['/donor/posts']);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving = true;
    this.postService.update(this.postId, this.form.value).subscribe({
      next: () => {
        this.saving = false;
        this.cdr.detectChanges();
        this.snackBar.open('¡Publicación actualizada!', 'OK', { duration: 3000 });
        this.router.navigate(['/donor/posts']);
      },
      error: (err) => {
        this.saving = false;
        this.cdr.detectChanges();
        this.snackBar.open(err.error?.message || 'Error al actualizar', 'OK', { duration: 3000 });
      }
    });
  }
}
