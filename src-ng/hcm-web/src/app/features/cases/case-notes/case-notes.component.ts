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
  templateUrl: './case-notes.component.html',
  styleUrls: ['./case-notes.component.scss']
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
