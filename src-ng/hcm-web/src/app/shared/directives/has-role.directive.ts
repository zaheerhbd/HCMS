import { Directive, Input, OnInit, TemplateRef, ViewContainerRef } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

// Structural directive that shows content only when the user has the required role(s).
// Usage: *appHasRole="'Admin'"  or  *appHasRole="['Admin', 'Supervisor']"
@Directive({
  selector: '[appHasRole]',
  standalone: true
})
export class HasRoleDirective implements OnInit {
  @Input() appHasRole: string | string[] = [];

  constructor(
    private templateRef: TemplateRef<unknown>,
    private viewContainer: ViewContainerRef,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const required = Array.isArray(this.appHasRole) ? this.appHasRole : [this.appHasRole];
    const hasRole = required.some(r => this.auth.hasRole(r));

    if (hasRole) {
      this.viewContainer.createEmbeddedView(this.templateRef);
    } else {
      this.viewContainer.clear();
    }
  }
}
