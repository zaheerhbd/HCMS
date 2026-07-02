import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CaseService } from '../cases/services/case.service';
import { DocumentDto } from '../../core/models/case.models';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.scss']
})
export class DocumentsComponent implements OnInit {
  caseNumberControl = new FormControl('', [Validators.required, Validators.pattern(/^CASE-\d{4}-\d{5}$/)]);
  documents: DocumentDto[] = [];
  loading = false;
  searched = false;
  activeCaseNumber = '';
  selectedFile: File | null = null;
  uploading = false;
  columns = ['icon', 'fileName', 'category', 'size', 'uploadedBy', 'uploadedAt', 'actions'];

  get canUpload(): boolean {
    return this.auth.hasRole('Admin') || this.auth.hasRole('CareCoordinator') || this.auth.hasRole('Supervisor');
  }

  constructor(
    private caseSvc: CaseService,
    private auth: AuthService,
    private snack: MatSnackBar
  ) {}

  ngOnInit(): void {}

  search(): void {
    if (this.caseNumberControl.invalid) return;
    const cn = this.caseNumberControl.value!.trim().toUpperCase();
    this.loading = true;
    this.searched = false;
    this.documents = [];
    this.activeCaseNumber = cn;

    this.caseSvc.getDocuments(cn).subscribe({
      next: docs => {
        this.documents = docs;
        this.loading = false;
        this.searched = true;
      },
      error: err => {
        const msg = err.status === 404 ? `Case ${cn} not found.` : 'Failed to load documents.';
        this.snack.open(msg, 'Close', { duration: 4000 });
        this.loading = false;
        this.searched = true;
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  upload(): void {
    if (!this.selectedFile || !this.activeCaseNumber || this.uploading) return;
    this.uploading = true;
    const fd = new FormData();
    fd.append('file', this.selectedFile);
    fd.append('category', 'Other');

    this.caseSvc.uploadDocument(this.activeCaseNumber, fd).subscribe({
      next: doc => {
        this.documents = [doc, ...this.documents];
        this.selectedFile = null;
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
    this.caseSvc.getDocumentDownloadUrl(this.activeCaseNumber, doc.id).subscribe({
      next: res => window.open(res.url, '_blank'),
      error: () => this.snack.open('Could not get download link.', 'Close', { duration: 3000 })
    });
  }

  deleteDoc(doc: DocumentDto): void {
    this.caseSvc.deleteDocument(this.activeCaseNumber, doc.id).subscribe({
      next: () => {
        this.documents = this.documents.filter(d => d.id !== doc.id);
        this.snack.open('Document deleted.', 'Close', { duration: 2000 });
      },
      error: () => this.snack.open('Failed to delete.', 'Close', { duration: 3000 })
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  iconForType(contentType: string): string {
    if (contentType === 'application/pdf') return 'picture_as_pdf';
    if (contentType.startsWith('image/')) return 'image';
    if (contentType.includes('word')) return 'description';
    return 'insert_drive_file';
  }
}
