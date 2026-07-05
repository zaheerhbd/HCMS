import { Component, OnInit, ViewChild, ElementRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ReportService, TasksByAssigneeReport } from '../report.service';
import { Chart, BarElement, BarController, CategoryScale, LinearScale, Tooltip, Legend } from 'chart.js';

Chart.register(BarElement, BarController, CategoryScale, LinearScale, Tooltip, Legend);

@Component({
  selector: 'app-tasks-by-assignee',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './tasks-by-assignee.component.html',
  styleUrls: ['./tasks-by-assignee.component.scss']
})
export class TasksByAssigneeComponent implements OnInit, OnDestroy {
  @ViewChild('stackedCanvas') stackedCanvas!: ElementRef<HTMLCanvasElement>;

  report: TasksByAssigneeReport | null = null;
  loading = true;
  error = '';
  private chart: Chart | null = null;
  columns = ['userName', 'totalTasks', 'doneTasks', 'inProgressTasks', 'overdueTasks', 'openTasks'];

  constructor(private svc: ReportService) {}

  ngOnInit(): void {
    this.svc.getTasksByAssignee().subscribe({
      next: r => { this.report = r; this.loading = false; setTimeout(() => this.buildChart(), 0); },
      error: () => { this.error = 'Failed to load report'; this.loading = false; }
    });
  }

  ngOnDestroy(): void { this.chart?.destroy(); }

  private buildChart(): void {
    if (!this.stackedCanvas || !this.report?.rows.length) return;
    this.chart = new Chart(this.stackedCanvas.nativeElement, {
      type: 'bar',
      data: {
        labels: this.report.rows.map(r => r.userName),
        datasets: [
          { label: 'Done',        data: this.report.rows.map(r => r.doneTasks),       backgroundColor: '#2e7d32' },
          { label: 'In Progress', data: this.report.rows.map(r => r.inProgressTasks), backgroundColor: '#1565c0' },
          { label: 'Open',        data: this.report.rows.map(r => r.openTasks),        backgroundColor: '#90a4ae' },
          { label: 'Overdue',     data: this.report.rows.map(r => r.overdueTasks),     backgroundColor: '#c62828' }
        ]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'top' } },
        scales: { x: { stacked: true }, y: { stacked: true, beginAtZero: true } }
      }
    });
  }

  exportCsv(): void { this.svc.downloadCsv('tasks-by-assignee'); }
}
