import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HasRoleDirective } from './has-role.directive';
import { AuthService } from '../../core/services/auth.service';

@Component({
  standalone: true,
  imports: [HasRoleDirective],
  template: `
    <div *appHasRole="'Admin'" class="single-role">Admin only</div>
    <div *appHasRole="['Admin', 'Supervisor']" class="multi-role">Admin or Supervisor</div>
  `
})
class TestHostComponent {}

describe('HasRoleDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let authSpy: jasmine.SpyObj<AuthService>;

  function build() {
    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  }

  beforeEach(() => {
    authSpy = jasmine.createSpyObj('AuthService', ['hasRole']);

    TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [{ provide: AuthService, useValue: authSpy }]
    });
  });

  it('renders single-role content when the user has the required role', () => {
    authSpy.hasRole.and.callFake((r: string) => r === 'Admin');
    build();

    expect(fixture.nativeElement.querySelector('.single-role')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.multi-role')).toBeTruthy();
  });

  it('hides content when the user lacks the required role', () => {
    authSpy.hasRole.and.returnValue(false);
    build();

    expect(fixture.nativeElement.querySelector('.single-role')).toBeNull();
    expect(fixture.nativeElement.querySelector('.multi-role')).toBeNull();
  });

  it('renders multi-role content when the user matches any one of the listed roles', () => {
    authSpy.hasRole.and.callFake((r: string) => r === 'Supervisor');
    build();

    expect(fixture.nativeElement.querySelector('.single-role')).toBeNull();
    expect(fixture.nativeElement.querySelector('.multi-role')).toBeTruthy();
  });
});
