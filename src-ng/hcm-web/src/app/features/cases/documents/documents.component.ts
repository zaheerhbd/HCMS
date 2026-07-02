import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CaseService } from '../services/case.service';
import { DocumentDto } from '../../../core/models/case.models';
import { AuthService } from '../../../core/services/auth.service';

const CATEGORIES = ['Referral', 'Lab Result', 'Consent Form', 'Insurance', 'Other'];

@Component({
  selector: 'app-case-documents',
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
    MatProgressBarModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.scss']
})
export class CaseDocumentsComponent implements OnInit {
  @Input() caseNumber = '';
  documents: DocumentDto[] = [];
  uploadForm: FormGroup;
  selectedFile: File | null = null;
  uploading = false;
  categories = CATEGORIES;
  columns = ['fileName', 'category', 'uploadedBy', 'actions'];

  get canUpload(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator') || this.auth.hasRole('Supervisor');
  }

  constructor(
    private fb: FormBuilder,
    private svc: CaseService,
    private auth: AuthService,
    private snack: MatSnackBar
  ) {
    this.uploadForm = this.fb.group({
      category: ['', Validators.required],
      description: ['']
    });
  }

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.svc.getDocuments(this.caseNumber).subscribe({
      next: docs => this.documents = docs,
      error: () => {}
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  upload(): void {
    if (!this.selectedFile || this.uploadForm.invalid || this.uploading) return;
    this.uploading = true;

    const fd = new FormData();
    fd.append('file', this.selectedFile);
    fd.append('category', this.uploadForm.value.category);
    if (this.uploadForm.value.description) fd.append('description', this.uploadForm.value.description);

    this.svc.uploadDocument(this.caseNumber, fd).subscribe({
      next: doc => {
        this.documents = [doc, ...this.documents];
        this.selectedFile = null;
        this.uploadForm.reset();
        this.uploading = false;
        this.snack.open('Document uploaded.', 'Close', { duration: 3000 });
      },
      error: err => {
        this.snack.open(err.error?.error ?? 'Upload failed.', 'Close', { duration: 4000 });
        this.uploading = false;
      }
    });
  }

  download(doc: DocumentDto): void {
    this.svc.getDocumentDownloadUrl(this.caseNumber, doc.id).subscribe({
      next: res => window.open(res.url, '_blank'),
      error: () => this.snack.open('Could not get download link.', 'Close', { duration: 3000 })
    });
  }

  deleteDoc(doc: DocumentDto): void {
    this.svc.deleteDocument(this.caseNumber, doc.id).subscribe({
      next: () => {
        this.documents = this.documents.filter(d => d.id !== doc.id);
        this.snack.open('Document deleted.', 'Close', { duration: 2000 });
      },
      error: () => this.snack.open('Failed to delete.', 'Close', { duration: 3000 })
    });
  }

  // Converts raw bytes to a human-readable size string (e.g. "1.4 MB")
  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  // Returns the appropriate Material icon name for common MIME types
  iconForType(contentType: string): string {
    if (contentType === 'application/pdf') return 'picture_as_pdf';
    if (contentType.startsWith('image/')) return 'image';
    if (contentType.includes('word')) return 'description';
    return 'insert_drive_file';
  }
}
