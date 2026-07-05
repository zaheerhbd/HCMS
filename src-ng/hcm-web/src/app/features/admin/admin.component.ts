import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatIconModule, MatButtonModule],
  template: `
    <div style="padding:24px">
      <h1 style="margin:0 0 24px">Administration</h1>
      <div style="display:flex;gap:16px;flex-wrap:wrap">
        <mat-card style="min-width:200px;cursor:pointer" routerLink="audit">
          <mat-card-content style="display:flex;align-items:center;gap:12px;padding:20px">
            <mat-icon style="font-size:32px;width:32px;height:32px;color:#1976d2">history</mat-icon>
            <div>
              <div style="font-weight:600">Audit Log</div>
              <div style="font-size:12px;color:#666">View system-wide audit trail</div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `
})
export class AdminComponent {}
