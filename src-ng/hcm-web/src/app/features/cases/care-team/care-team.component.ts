import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CaseService } from '../services/case.service';
import { CareTeamMemberDto } from '../../../core/models/case.models';
import { AuthService } from '../../../core/services/auth.service';
import { UserService, AssignableUser } from '../../../core/services/user.service';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-care-team',
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
    MatSnackBarModule
  ],
  templateUrl: './care-team.component.html',
  styleUrls: ['./care-team.component.scss']
})
export class CareTeamComponent implements OnInit {
  @Input() caseNumber = '';
  members: CareTeamMemberDto[] = [];
  assignableUsers: AssignableUser[] = [];
  addForm: FormGroup;
  adding = false;
  columns = ['user', 'teamRole', 'joinedAt', 'actions'];

  get canManage(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator') || this.auth.hasRole('Supervisor');
  }

  constructor(
    private fb: FormBuilder,
    private svc: CaseService,
    private userSvc: UserService,
    private dialog: MatDialog,
    private snack: MatSnackBar,
    private auth: AuthService
  ) {
    this.addForm = this.fb.group({
      userId: ['', Validators.required],
      teamRole: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadTeam();
    this.userSvc.getAssignable().subscribe({ next: u => this.assignableUsers = u, error: () => {} });
  }

  loadTeam(): void {
    this.svc.getTeam(this.caseNumber).subscribe({
      next: t => this.members = t,
      error: () => {}
    });
  }

  addMember(): void {
    if (this.addForm.invalid || this.adding) return;
    this.adding = true;
    this.svc.addTeamMember(this.caseNumber, this.addForm.value).subscribe({
      next: m => {
        this.members = [...this.members, m];
        this.addForm.reset();
        this.adding = false;
        this.snack.open('Member added.', 'Close', { duration: 3000 });
      },
      error: err => {
        this.snack.open(err.error?.error ?? 'Failed to add member.', 'Close', { duration: 4000 });
        this.adding = false;
      }
    });
  }

  removeMember(member: CareTeamMemberDto): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Remove Team Member',
        message: `Remove ${member.userName} from the care team?`,
        confirmLabel: 'Remove'
      }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.svc.removeTeamMember(this.caseNumber, member.userId).subscribe({
        next: () => {
          this.members = this.members.map(m =>
            m.userId === member.userId ? { ...m, isActive: false } : m
          );
          this.snack.open('Member removed.', 'Close', { duration: 3000 });
        },
        error: () => this.snack.open('Failed to remove member.', 'Close', { duration: 4000 })
      });
    });
  }
}
