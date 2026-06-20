import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { DeliveryService } from '../../../services/Delivery.service';
import { ZoneService } from '../../../services/Zone.service';
import { AuthService } from '../../../services/auth.service';
import { Zone } from '../../../models/Zone.model';

@Component({
  selector: 'app-delivery',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatProgressSpinnerModule, MatSnackBarModule, MatStepperModule
  ],
  templateUrl: './delivery.html',
  styleUrl: './delivery.css'
})
export class DeliveryComponent implements OnInit {
  form: FormGroup;
  zones: Zone[] = [];
  loading = false;
  confirming = false;
  postId = '';
  deliveryCreated = false;
  donorConfirmed = false;
  receiverConfirmed = false;
  deliveryLocation = '';
  deliveryDate = '';

  constructor(
    private fb: FormBuilder,
    private deliveryService: DeliveryService,
    private zoneService: ZoneService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      deliveryDate: ['', Validators.required],
      zone: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.postId = this.route.snapshot.paramMap.get('postId')!;
    this.zoneService.getAll().subscribe(z => {
      this.zones = z;
      this.cdr.detectChanges();
    });
    this.loadDeliveryStatus();
  }

  loadDeliveryStatus(): void {
    this.deliveryService.getByPost(this.postId).subscribe({
      next: (record: any) => {
        if (record) {
          this.deliveryCreated = true;
          this.donorConfirmed = record.confirmedByDonor ?? record.ConfirmedByDonor ?? false;
          this.receiverConfirmed = record.confirmedByReceiver ?? record.ConfirmedByReceiver ?? false;
          this.deliveryLocation = record.deliveryLocation ?? record.DeliveryLocation ?? '';
          this.deliveryDate = record.deliveryDate ?? record.DeliveryDate ?? '';
          this.cdr.detectChanges();
        }
      },
      error: () => {}
    });
  }

  getSelectedZoneAddress(): string {
    const zone = this.zones.find(z => z.name === this.form.get('zone')?.value);
    return zone ? zone.address : '';
  }

  createDelivery(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const zone = this.zones.find(z => z.name === this.form.get('zone')?.value);
    const data = {
      postId: this.postId,
      deliveryDate: new Date(this.form.get('deliveryDate')?.value).toISOString(),
      deliveryLocation: zone ? `${zone.name} — ${zone.address}` : this.form.get('zone')?.value
    };
    this.deliveryService.create(data).subscribe({
      next: () => {
        this.loading = false;
        this.deliveryCreated = true;
        this.cdr.detectChanges();
        this.snackBar.open('Entrega registrada. Ahora confirma cuando la hayas realizado.', 'OK', { duration: 4000 });
      },
      error: (err) => {
        this.loading = false;
        this.cdr.detectChanges();
        this.snackBar.open(err.error?.message || 'Error al registrar entrega', 'OK', { duration: 3000 });
      }
    });
  }

  confirmDelivery(): void {
    if (!confirm('¿Confirmas que ya entregaste el artículo?')) return;
    this.confirming = true;
    this.deliveryService.confirmDonor(this.postId).subscribe({
      next: () => {
        this.confirming = false;
        this.donorConfirmed = true;
        this.cdr.detectChanges();
        this.snackBar.open('¡Entrega confirmada! Esperando confirmación del receptor.', 'OK', { duration: 4000 });
      },
      error: (err) => {
        this.confirming = false;
        this.cdr.detectChanges();
        this.snackBar.open(err.error?.message || 'Error al confirmar', 'OK', { duration: 3000 });
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
