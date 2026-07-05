import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ReportService, CasesByStatusReport } from '../report.service';
import { Chart, ArcElement, DoughnutController, Tooltip, Legend } from 'chart.js';

Chart.register(ArcElement, DoughnutController, Tooltip, Legend);

@Component({
  selector: 'app-cases-by-status',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './cases-by-status.component.html',
  styleUrls: ['./cases-by-status.component.scss']
})
export class CasesByStatusComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  report: CasesByStatusReport | null = null;
  loading = true;
  error = '';
  private chart: Chart | null = null;
  columns = ['status', 'count', 'percentage'];

  constructor(private svc: ReportService) {}

  ngOnInit(): void {
    this.svc.getCasesByStatus().subscribe({
      next: r => { this.report = r; this.loading = false; setTimeout(() => this.buildChart(), 0); },
      error: () => { this.error = 'Failed to load report'; this.loading = false; }
    });
  }

  ngAfterViewInit(): void {}
  ngOnDestroy(): void { this.chart?.destroy(); }

  private buildChart(): void {
    if (!this.chartCanvas || !this.report?.rows.length) return;
    const colors = ['#1565c0','#2e7d32','#e65100','#6a1b9a','#c62828','#00695c','#455a64'];
    this.chart = new Chart(this.chartCanvas.nativeElement, {
      type: 'doughnut',
      data: {
        labels: this.report.rows.map(r => r.status),
        datasets: [{ data: this.report.rows.map(r => r.count), backgroundColor: colors, borderWidth: 2 }]
      },
      options: { responsive: true, plugins: { legend: { position: 'right' } }, cutout: '60%' }
    });
  }

  exportCsv(): void { this.svc.downloadCsv('cases-by-status'); }
}
