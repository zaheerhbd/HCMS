import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TaskService } from './task.service';
import { CaseService } from '../cases/services/case.service';
import { TaskDto } from '../../core/models/case.models';

const STATUS_COLORS: Record<string, string> = {
  Todo: '#9e9e9e',
  InProgress: '#1976d2',
  Blocked: '#f44336',
  Done: '#388e3c',
  Cancelled: '#757575'
};

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './tasks.component.html',
  styleUrls: ['./tasks.component.scss']
})
export class TasksComponent implements OnInit {
  tasks: TaskDto[] = [];
  loading = false;
  columns = ['title', 'caseNumber', 'priority', 'status', 'dueDate'];
  statuses = ['Todo', 'InProgress', 'Blocked', 'Done', 'Cancelled'];

  get overdueCount(): number {
    return this.tasks.filter(t => this.isOverdue(t)).length;
  }

  constructor(
    private taskSvc: TaskService,
    private caseSvc: CaseService,
    private snack: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.taskSvc.getMyTasks().subscribe({
      next: tasks => {
        this.tasks = tasks;
        this.loading = false;
      },
      error: () => {
        this.snack.open('Failed to load tasks.', 'Close', { duration: 3000 });
        this.loading = false;
      }
    });
  }

  changeStatus(task: TaskDto, newStatus: string): void {
    if (!task.caseNumber) return;
    this.caseSvc.changeTaskStatus(task.caseNumber, task.id, { newStatus }).subscribe({
      next: () => {
        // Remove from list when done or cancelled — task no longer needs action
        if (newStatus === 'Done' || newStatus === 'Cancelled') {
          this.tasks = this.tasks.filter(t => t.id !== task.id);
        } else {
          this.tasks = this.tasks.map(t => t.id === task.id ? { ...t, status: newStatus } : t);
        }
        this.snack.open(`Task marked ${newStatus}.`, 'Close', { duration: 2000 });
      },
      error: () => this.snack.open('Failed to update status.', 'Close', { duration: 3000 })
    });
  }

  statusColor(status: string): string {
    return STATUS_COLORS[status] ?? '#757575';
  }

  isOverdue(task: TaskDto): boolean {
    if (!task.dueDate || task.status === 'Done' || task.status === 'Cancelled') return false;
    return new Date(task.dueDate) < new Date();
  }
}
