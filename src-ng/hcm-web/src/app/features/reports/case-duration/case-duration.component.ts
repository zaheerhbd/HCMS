import { Component, OnInit, ViewChild, ElementRef, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ReportService, CaseDurationReport } from '../report.service';
import { Chart, BarElement, BarController, CategoryScale, LinearScale, Tooltip, Legend } from 'chart.js';

Chart.register(BarElement, BarController, CategoryScale, LinearScale, Tooltip, Legend);

@Component({
  selector: 'app-case-duration',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './case-duration.component.html',
  styleUrls: ['./case-duration.component.scss']
})
export class CaseDurationComponent implements OnInit, OnDestroy {
  @ViewChild('barCanvas') barCanvas!: ElementRef<HTMLCanvasElement>;

  report: CaseDurationReport | null = null;
  loading = true;
  error = '';
  private chart: Chart | null = null;
  columns = ['caseType', 'caseCount', 'averageDays', 'minDays', 'maxDays'];

  constructor(private svc: ReportService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.svc.getCaseDuration().subscribe({
      next: r => { this.report = r; this.loading = false; this.cdr.detectChanges(); this.buildChart(); },
      error: () => { this.error = 'Failed to load report'; this.loading = false; }
    });
  }

  ngOnDestroy(): void { this.chart?.destroy(); }

  private buildChart(): void {
    if (!this.barCanvas || !this.report?.rows.length) return;
    this.chart = new Chart(this.barCanvas.nativeElement, {
      type: 'bar',
      data: {
        labels: this.report.rows.map(r => r.caseType),
        datasets: [{
          label: 'Avg Days to Close',
          data: this.report.rows.map(r => r.averageDays),
          backgroundColor: '#1565c0'
        }]
      },
      options: { responsive: true, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } }
    });
  }

  exportCsv(): void { this.svc.downloadCsv('case-duration'); }
}
