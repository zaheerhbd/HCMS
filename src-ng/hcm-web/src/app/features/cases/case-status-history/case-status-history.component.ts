import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge.component';
import { CaseStatusHistoryDto } from '../../../core/models/case.models';

@Component({
  selector: 'app-case-status-history',
  standalone: true,
  imports: [CommonModule, MatIconModule, StatusBadgeComponent],
  template: `
    <div class="timeline">
      <div class="timeline-item" *ngFor="let entry of history">
        <div class="timeline-dot"></div>
        <div class="timeline-content">
          <div class="timeline-header">
            <span *ngIf="entry.fromStatus" class="transition">
              <app-status-badge [status]="entry.fromStatus"></app-status-badge>
              <mat-icon style="vertical-align:middle;color:#757575">arrow_forward</mat-icon>
            </span>
            <app-status-badge [status]="entry.toStatus"></app-status-badge>
          </div>
          <div class="timeline-meta">
            {{ entry.changedByUserName }} · {{ entry.changedAt | date:'medium' }}
          </div>
          <div *ngIf="entry.comment" class="timeline-comment">{{ entry.comment }}</div>
        </div>
      </div>
      <div *ngIf="history.length === 0" class="empty">No status history.</div>
    </div>
  `,
  styles: [`
    .timeline { padding: 8px 0; }
    .timeline-item { display: flex; gap: 12px; margin-bottom: 20px; }
    .timeline-dot { width: 12px; height: 12px; border-radius: 50%; background: #1976d2; margin-top: 6px; flex-shrink: 0; }
    .timeline-content { flex: 1; }
    .timeline-header { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .transition { display: flex; align-items: center; gap: 4px; }
    .timeline-meta { font-size: 12px; color: #757575; margin-top: 4px; }
    .timeline-comment { font-size: 14px; color: #424242; margin-top: 4px; font-style: italic; }
    .empty { color: #757575; text-align: center; padding: 20px; }
  `]
})
export class CaseStatusHistoryComponent {
  @Input() history: CaseStatusHistoryDto[] = [];
}
