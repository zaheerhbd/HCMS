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
  template: `
    <div *ngIf="canManage" class="add-form">
      <form [formGroup]="addForm" (ngSubmit)="addMember()">
        <mat-form-field appearance="outline">
          <mat-label>Team Member</mat-label>
          <mat-select formControlName="userId">
            <mat-option *ngFor="let u of assignableUsers" [value]="u.id">
              {{ u.fullName }} <span class="role-hint">({{ u.roles[0] }})</span>
            </mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Role on Team</mat-label>
          <mat-select formControlName="teamRole">
            <mat-option value="Lead Coordinator">Lead Coordinator</mat-option>
            <mat-option value="Care Coordinator">Care Coordinator</mat-option>
            <mat-option value="Clinician">Clinician</mat-option>
            <mat-option value="Specialist">Specialist</mat-option>
            <mat-option value="Support">Support</mat-option>
          </mat-select>
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="addForm.invalid || adding">
          Add Member
        </button>
      </form>
    </div>

    <table mat-table [dataSource]="members" *ngIf="members.length > 0">
      <ng-container matColumnDef="user">
        <th mat-header-cell *matHeaderCellDef>User</th>
        <td mat-cell *matCellDef="let m">
          <div class="user-cell">
            <span class="user-name">{{ m.userName }}</span>
            <span class="user-email">{{ m.userEmail }}</span>
          </div>
        </td>
      </ng-container>
      <ng-container matColumnDef="teamRole">
        <th mat-header-cell *matHeaderCellDef>Role</th>
        <td mat-cell *matCellDef="let m">{{ m.teamRole }}</td>
      </ng-container>
      <ng-container matColumnDef="joinedAt">
        <th mat-header-cell *matHeaderCellDef>Joined</th>
        <td mat-cell *matCellDef="let m">{{ m.joinedAt | date:'mediumDate' }}</td>
      </ng-container>
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef></th>
        <td mat-cell *matCellDef="let m">
          <button mat-icon-button color="warn" (click)="removeMember(m)" *ngIf="canManage && m.isActive">
            <mat-icon>person_remove</mat-icon>
          </button>
        </td>
      </ng-container>
      <tr mat-header-row *matHeaderRowDef="columns"></tr>
      <tr mat-row *matRowDef="let row; columns: columns;"
        [class.inactive]="!row.isActive"></tr>
    </table>
    <div *ngIf="members.length === 0" class="empty">No team members assigned.</div>
  `,
  styles: [`
    .add-form form { display: flex; gap: 12px; align-items: flex-start; margin-bottom: 16px; flex-wrap: wrap; }
    .add-form mat-form-field { flex: 1; min-width: 180px; }
    .role-hint { font-size: 12px; color: #757575; }
    .user-cell { display: flex; flex-direction: column; }
    .user-name { font-weight: 500; }
    .user-email { font-size: 12px; color: #757575; }
    .inactive { opacity: 0.5; }
    .empty { color: #757575; text-align: center; padding: 20px; }
    table { width: 100%; }
  `]
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
