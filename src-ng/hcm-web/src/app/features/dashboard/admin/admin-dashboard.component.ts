import { Component, OnInit, AfterViewInit, ElementRef, ViewChild, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardService, AdminDashboard } from '../dashboard.service';
import { Chart, LineElement, LineController, PointElement, CategoryScale, LinearScale, Tooltip, Legend, BarElement, BarController } from 'chart.js';

Chart.register(LineElement, LineController, PointElement, CategoryScale, LinearScale, Tooltip, Legend, BarElement, BarController);

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatTableModule, MatProgressSpinnerModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  @ViewChild('lineCanvas') lineCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('roleCanvas') roleCanvas!: ElementRef<HTMLCanvasElement>;

  data: AdminDashboard | null = null;
  loading = true;
  error = '';
  private lineChart: Chart | null = null;
  private roleChart: Chart | null = null;

  roleColumns = ['role', 'count'];

  constructor(private svc: DashboardService) {}

  ngOnInit(): void {
    this.svc.getAdmin().subscribe({
      next: d => {
        this.data = d;
        this.loading = false;
        setTimeout(() => this.buildCharts(), 0);
      },
      error: () => { this.error = 'Failed to load admin dashboard'; this.loading = false; }
    });
  }

  ngOnDestroy(): void { this.lineChart?.destroy(); this.roleChart?.destroy(); }

  private buildCharts(): void {
    if (!this.data) return;

    if (this.lineCanvas && this.data.loginTrend.length) {
      this.lineChart = new Chart(this.lineCanvas.nativeElement, {
        type: 'line',
        data: {
          labels: this.data.loginTrend.map(d => d.date),
          datasets: [{
            label: 'Unique Logins',
            data: this.data.loginTrend.map(d => d.count),
            borderColor: '#1565c0',
            backgroundColor: 'rgba(21,101,192,.12)',
            tension: 0.35,
            fill: true,
            pointRadius: 4
          }]
        },
        options: { responsive: true, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } }
      });
    }

    if (this.roleCanvas && this.data.usersByRole.length) {
      this.roleChart = new Chart(this.roleCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: this.data.usersByRole.map(r => r.role),
          datasets: [{
            label: 'Users',
            data: this.data.usersByRole.map(r => r.count),
            backgroundColor: ['#1565c0','#2e7d32','#e65100','#6a1b9a','#c62828']
          }]
        },
        options: { responsive: true, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } }
      });
    }
  }
}
