import { Component, OnInit, ElementRef, ViewChild, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { DashboardService, CoordinatorDashboard } from '../dashboard.service';
import { Chart, ArcElement, Tooltip, Legend, DoughnutController } from 'chart.js';

Chart.register(ArcElement, Tooltip, Legend, DoughnutController);

@Component({
  selector: 'app-coordinator-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatIconModule, MatTableModule, MatProgressSpinnerModule, MatChipsModule],
  templateUrl: './coordinator-dashboard.component.html',
  styleUrls: ['./coordinator-dashboard.component.scss']
})
export class CoordinatorDashboardComponent implements OnInit, OnDestroy {
  @ViewChild('donutCanvas') donutCanvas!: ElementRef<HTMLCanvasElement>;

  data: CoordinatorDashboard | null = null;
  loading = true;
  error = '';
  private chart: Chart | null = null;

  overdueColumns = ['title', 'caseNumber', 'priority', 'dueDate'];
  apptColumns    = ['time', 'patientName', 'mrn', 'type', 'status'];

  constructor(private svc: DashboardService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.svc.getCoordinator().subscribe({
      next: d => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        this.buildChart();
      },
      error: () => { this.error = 'Failed to load dashboard'; this.loading = false; }
    });
  }

  ngOnDestroy(): void { this.chart?.destroy(); }

  private buildChart(): void {
    if (!this.donutCanvas || !this.data?.casesByStatus.length) return;
    const labels = this.data.casesByStatus.map(s => s.status);
    const values = this.data.casesByStatus.map(s => s.count);
    const colors = ['#1565c0','#2e7d32','#e65100','#6a1b9a','#c62828','#00695c','#455a64'];

    this.chart = new Chart(this.donutCanvas.nativeElement, {
      type: 'doughnut',
      data: {
        labels,
        datasets: [{ data: values, backgroundColor: colors.slice(0, values.length), borderWidth: 2 }]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'right' } },
        cutout: '65%'
      }
    });
  }

  priorityClass(p: string): string {
    return p === 'High' ? 'chip-red' : p === 'Medium' ? 'chip-orange' : 'chip-gray';
  }

  isOverdue(d: string): boolean { return new Date(d) < new Date(); }
}
