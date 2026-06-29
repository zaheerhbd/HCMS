export interface PatientInsuranceDto {
  id: string;
  insurancePlan: string;
  memberId: string;
  groupNumber?: string;
  subscriberName?: string;
  effectiveDate: string;
  terminationDate?: string;
  isPrimary: boolean;
  createdAt: string;
}

export interface PatientDto {
  id: string;
  mrn: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender?: string;
  phone?: string;
  email?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  insurance: PatientInsuranceDto[];
}

export interface PatientListItemDto {
  id: string;
  mrn: string;
  fullName: string;
  dateOfBirth: string;
  phone?: string;
  email?: string;
}

export interface PatientSearchResult {
  totalCount: number;
  page: number;
  pageSize: number;
  results: PatientListItemDto[];
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender?: string;
  phone?: string;
  email?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
}

export interface UpdatePatientRequest {
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string;
  gender?: string;
  phone?: string;
  email?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
}
