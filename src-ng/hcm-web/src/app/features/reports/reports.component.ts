import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, MatTabsModule],
  template: `
    <nav mat-tab-nav-bar [tabPanel]="tabPanel" style="padding:0 24px;background:white;border-bottom:1px solid #dde3e8;">
      <a mat-tab-link routerLink="cases-by-status"  routerLinkActive #r1="routerLinkActive" [active]="r1.isActive">Cases by Status</a>
      <a mat-tab-link routerLink="case-duration"    routerLinkActive #r2="routerLinkActive" [active]="r2.isActive">Case Duration</a>
      <a mat-tab-link routerLink="tasks-by-assignee" routerLinkActive #r3="routerLinkActive" [active]="r3.isActive">Tasks by Assignee</a>
    </nav>
    <mat-tab-nav-panel #tabPanel><router-outlet></router-outlet></mat-tab-nav-panel>
  `
})
export class ReportsComponent {}
