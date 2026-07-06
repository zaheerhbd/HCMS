import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { PatientFormComponent } from './patient-form.component';

describe('PatientFormComponent — form validation', () => {
  let fixture: ComponentFixture<PatientFormComponent>;
  let component: PatientFormComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PatientFormComponent, HttpClientTestingModule],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ mrn: 'new' }) }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PatientFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be invalid when required fields are empty', () => {
    expect(component.form.valid).toBeFalse();
  });

  it('should require firstName, lastName, and dateOfBirth', () => {
    expect(component.form.get('firstName')?.hasError('required')).toBeTrue();
    expect(component.form.get('lastName')?.hasError('required')).toBeTrue();
    expect(component.form.get('dateOfBirth')?.hasError('required')).toBeTrue();
  });

  it('should become valid once all required fields are filled', () => {
    component.form.patchValue({
      firstName: 'Jane',
      lastName: 'Doe',
      dateOfBirth: new Date('1990-01-01')
    });
    expect(component.form.valid).toBeTrue();
  });

  it('should not reject a date of birth in the future (documents a known gap — see FR validation)', () => {
    const futureDob = new Date(Date.now() + 1000 * 60 * 60 * 24 * 365);
    component.form.patchValue({
      firstName: 'Jane',
      lastName: 'Doe',
      dateOfBirth: futureDob
    });
    // NOTE: the form currently has no max-date validator, so a future DOB
    // passes validation. This test documents the current (incorrect) behavior;
    // flip this expectation once a dateOfBirth <= today validator is added.
    expect(component.form.valid).toBeTrue();
  });

  it('should leave optional fields (phone, email, address) valid when empty', () => {
    component.form.patchValue({
      firstName: 'Jane',
      lastName: 'Doe',
      dateOfBirth: new Date('1990-01-01')
    });
    expect(component.form.get('phone')?.valid).toBeTrue();
    expect(component.form.get('email')?.valid).toBeTrue();
  });
});
