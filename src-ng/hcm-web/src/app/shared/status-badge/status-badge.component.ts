import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule, MatChipsModule],
  template: `
    <mat-chip [style.background-color]="color" [style.color]="'white'" [style.font-size]="'12px'">
      {{ status }}
    </mat-chip>
  `
})
export class StatusBadgeComponent {
  @Input() status = '';

  get color(): string {
    switch (this.status?.toLowerCase()) {
      case 'open':       return '#1976d2';
      case 'inprogress': return '#f57c00';
      case 'onhold':     return '#757575';
      case 'closed':     return '#d32f2f';
      case 'reopened':   return '#7b1fa2';
      default:           return '#9e9e9e';
    }
  }
}
