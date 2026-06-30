import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CaseService } from '../services/case.service';
import { CaseNoteDto } from '../../../core/models/case.models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-case-notes',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatSnackBarModule
  ],
  template: `
    <!-- Add note form -->
    <div class="add-note" *ngIf="canAddNote">
      <mat-form-field appearance="outline" class="note-field">
        <mat-label>Add a note…</mat-label>
        <textarea matInput [formControl]="noteControl" rows="3" maxlength="5000"></textarea>
        <mat-hint align="end">{{ noteControl.value?.length ?? 0 }} / 5000</mat-hint>
      </mat-form-field>
      <button mat-flat-button color="primary" [disabled]="noteControl.invalid || saving"
        (click)="addNote()">
        <mat-icon>send</mat-icon> Post Note
      </button>
    </div>

    <!-- Note list -->
    <mat-card *ngFor="let note of notes" class="note-card" [class.locked]="!note.isEditable">
      <mat-card-header>
        <mat-card-subtitle>
          {{ note.createdByUserName }} · {{ note.createdAt | date:'medium' }}
          <span *ngIf="!note.isEditable" class="locked-badge">
            <mat-icon style="font-size:14px;vertical-align:middle">lock</mat-icon> Read-only
          </span>
        </mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <p>{{ note.content }}</p>
      </mat-card-content>
    </mat-card>

    <div *ngIf="notes.length === 0" class="empty">No notes yet.</div>
  `,
  styles: [`
    .add-note { display: flex; flex-direction: column; gap: 8px; margin-bottom: 20px; }
    .note-field { width: 100%; }
    .note-card { margin-bottom: 12px; }
    .note-card.locked { background: #fafafa; }
    .locked-badge { color: #757575; font-size: 12px; margin-left: 8px; }
    .empty { color: #757575; text-align: center; padding: 20px; }
  `]
})
export class CaseNotesComponent implements OnInit {
  @Input() caseNumber = '';
  notes: CaseNoteDto[] = [];
  noteControl = new FormControl('', [Validators.required, Validators.maxLength(5000)]);
  saving = false;

  get canAddNote(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator') || this.auth.hasRole('Clinician');
  }

  constructor(
    private svc: CaseService,
    private snack: MatSnackBar,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.loadNotes();
  }

  loadNotes(): void {
    this.svc.getNotes(this.caseNumber).subscribe({
      next: n => this.notes = n,
      error: () => {}
    });
  }

  addNote(): void {
    if (this.noteControl.invalid || this.saving) return;
    this.saving = true;
    this.svc.addNote(this.caseNumber, { content: this.noteControl.value! }).subscribe({
      next: n => {
        this.notes = [n, ...this.notes];
        this.noteControl.reset();
        this.saving = false;
        this.snack.open('Note added.', 'Close', { duration: 3000 });
      },
      error: err => {
        this.snack.open(err.error?.error ?? 'Failed to add note.', 'Close', { duration: 4000 });
        this.saving = false;
      }
    });
  }
}
