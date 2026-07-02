import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge.component';
import { CaseStatusHistoryDto } from '../../../core/models/case.models';

@Component({
  selector: 'app-case-status-history',
  standalone: true,
  imports: [CommonModule, MatIconModule, StatusBadgeComponent],
  templateUrl: './case-status-history.component.html',
  styleUrls: ['./case-status-history.component.scss']
})
export class CaseStatusHistoryComponent {
  @Input() history: CaseStatusHistoryDto[] = [];
}
