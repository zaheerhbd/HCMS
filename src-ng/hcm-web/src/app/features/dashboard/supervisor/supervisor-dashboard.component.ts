import { Component, OnInit, AfterViewInit, ElementRef, ViewChild, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardService, SupervisorDashboard } from '../dashboard.service';
import { Chart, BarElement, BarController, CategoryScale, LinearScale, Tooltip, Legend } from 'chart.js';

Chart.register(BarElement, BarController, CategoryScale, LinearScale, Tooltip, Legend);

@Component({
  selector: 'app-supervisor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatIconModule, MatTableModule, MatProgressSpinnerModule],
  templateUrl: './supervisor-dashboard.component.html',
  styleUrls: ['./supervisor-dashboard.component.scss']
})
export class SupervisorDashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('barCanvas') barCanvas!: ElementRef<HTMLCanvasElement>;

  data: SupervisorDashboard | null = null;
  loading = true;
  error = '';
  private chart: Chart | null = null;

  escalatedColumns = ['caseNumber', 'patientName', 'assignedTo', 'escalatedAt'];
  inactiveColumns  = ['caseNumber', 'patientName', 'assignedTo', 'daysSinceActivity'];

  constructor(private svc: DashboardService) {}

  ngOnInit(): void {
    this.svc.getSupervisor().subscribe({
      next: d => { this.data = d; this.loading = false; setTimeout(() => this.buildChart(), 0); },
      error: () => { this.error = 'Failed to load dashboard'; this.loading = false; }
    });
  }

  ngAfterViewInit(): void {}
  ngOnDestroy(): void { this.chart?.destroy(); }

  private buildChart(): void {
    if (!this.barCanvas || !this.data?.teamCaseloads.length) return;
    const labels = this.data.teamCaseloads.map(u => u.userName);
    this.chart = new Chart(this.barCanvas.nativeElement, {
      type: 'bar',
      data: {
        labels,
        datasets: [
          { label: 'Open Cases', data: this.data.teamCaseloads.map(u => u.openCases), backgroundColor: '#1565c0' },
          { label: 'Overdue Tasks', data: this.data.teamCaseloads.map(u => u.overdueTasks), backgroundColor: '#c62828' }
        ]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'top' } },
        scales: { x: { stacked: false }, y: { beginAtZero: true } }
      }
    });
  }
}
