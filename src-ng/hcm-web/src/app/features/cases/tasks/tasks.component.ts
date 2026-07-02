import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CaseService } from '../services/case.service';
import { TaskDto } from '../../../core/models/case.models';
import { AuthService } from '../../../core/services/auth.service';
import { UserService, AssignableUser } from '../../../core/services/user.service';

// Colour map for the inline status dropdown text colour
const STATUS_COLORS: Record<string, string> = {
  Todo: '#9e9e9e',
  InProgress: '#1976d2',
  Blocked: '#f44336',
  Done: '#388e3c',
  Cancelled: '#757575'
};

@Component({
  selector: 'app-case-tasks',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './tasks.component.html',
  styleUrls: ['./tasks.component.scss']
})
export class CaseTasksComponent implements OnInit {
  @Input() caseNumber = '';
  tasks: TaskDto[] = [];
  assignableUsers: AssignableUser[] = [];
  addForm: FormGroup;
  adding = false;
  columns = ['title', 'priority', 'status', 'dueDate', 'actions'];
  statuses = ['Todo', 'InProgress', 'Blocked', 'Done', 'Cancelled'];

  get canManage(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator') || this.auth.hasRole('Supervisor');
  }

  constructor(
    private fb: FormBuilder,
    private svc: CaseService,
    private auth: AuthService,
    private userSvc: UserService,
    private snack: MatSnackBar
  ) {
    this.addForm = this.fb.group({
      title: ['', Validators.required],
      priority: ['Medium'],
      assignedTo: [null],
      dueDate: [null]
    });
  }

  ngOnInit(): void {
    this.loadTasks();
    this.userSvc.getAssignable().subscribe({ next: u => this.assignableUsers = u, error: () => {} });
  }

  loadTasks(): void {
    this.svc.getTasks(this.caseNumber).subscribe({
      next: t => this.tasks = t,
      error: () => {}
    });
  }

  createTask(): void {
    if (this.addForm.invalid || this.adding) return;
    this.adding = true;
    const val = this.addForm.value;
    this.svc.createTask(this.caseNumber, {
      title: val.title,
      priority: val.priority,
      assignedTo: val.assignedTo || undefined,
      dueDate: val.dueDate || undefined
    }).subscribe({
      next: t => {
        this.tasks = [...this.tasks, t];
        this.addForm.reset({ priority: 'Medium', assignedTo: null, dueDate: null });
        this.adding = false;
        this.snack.open('Task added.', 'Close', { duration: 3000 });
      },
      error: err => {
        this.snack.open(err.error?.error ?? 'Failed to add task.', 'Close', { duration: 4000 });
        this.adding = false;
      }
    });
  }

  changeStatus(task: TaskDto, newStatus: string): void {
    this.svc.changeTaskStatus(this.caseNumber, task.id, { newStatus }).subscribe({
      next: updated => {
        this.tasks = this.tasks.map(t => t.id === updated.id ? updated : t);
        this.snack.open(`Task marked ${newStatus}.`, 'Close', { duration: 2000 });
      },
      error: () => this.snack.open('Failed to update status.', 'Close', { duration: 3000 })
    });
  }

  cancelTask(task: TaskDto): void {
    this.svc.deleteTask(this.caseNumber, task.id).subscribe({
      next: () => {
        this.tasks = this.tasks.map(t => t.id === task.id ? { ...t, status: 'Cancelled' } : t);
        this.snack.open('Task cancelled.', 'Close', { duration: 2000 });
      },
      error: () => this.snack.open('Failed to cancel task.', 'Close', { duration: 3000 })
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
