import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { CaseFormComponent } from './case-form.component';

describe('CaseFormComponent — form validation', () => {
  let fixture: ComponentFixture<CaseFormComponent>;
  let component: CaseFormComponent;

  function build(queryParams: Record<string, string> = {}) {
    TestBed.configureTestingModule({
      imports: [CaseFormComponent, HttpClientTestingModule],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(queryParams) }
          }
        }
      ]
    });
    fixture = TestBed.createComponent(CaseFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('should be invalid when patientMrn and caseTypeId are empty', () => {
    build();
    expect(component.form.valid).toBeFalse();
    expect(component.form.get('patientMrn')?.hasError('required')).toBeTrue();
    expect(component.form.get('caseTypeId')?.hasError('required')).toBeTrue();
  });

  it('should be valid once patientMrn and caseTypeId are set (notes is optional)', () => {
    build();
    component.form.patchValue({ patientMrn: 'MRN-2026-00001', caseTypeId: 1 });
    expect(component.form.valid).toBeTrue();
  });

  it('should pre-fill patientMrn from the patientMrn query param', () => {
    build({ patientMrn: 'MRN-2026-00042' });
    expect(component.form.get('patientMrn')?.value).toBe('MRN-2026-00042');
  });

  it('should expose the five seeded case types', () => {
    build();
    expect(component.caseTypes.length).toBe(5);
    expect(component.caseTypes.map(t => t.name)).toContain('Chronic Disease');
  });
});
