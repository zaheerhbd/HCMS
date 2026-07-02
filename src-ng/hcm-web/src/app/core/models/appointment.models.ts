export interface AppointmentDto {
  id: string;
  patientId: string;
  patientName: string;
  patientMrn: string;
  caseId?: string;
  caseNumber?: string;
  providerId: string;
  providerName: string;
  appointmentType: string;
  status: string;
  scheduledStart: string;
  scheduledEnd: string;
  location?: string;
  notes?: string;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface AppointmentListResult {
  items: AppointmentDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateAppointmentRequest {
  patientMrn: string;
  caseNumber?: string;
  providerId: string;
  appointmentType: string;
  scheduledStart: string;
  scheduledEnd: string;
  location?: string;
  notes?: string;
}

export interface UpdateAppointmentRequest {
  appointmentType?: string;
  scheduledStart?: string;
  scheduledEnd?: string;
  location?: string;
  notes?: string;
}

export interface AppointmentStatusChangeRequest {
  newStatus: string;
}

export interface ProviderDto {
  id: string;
  userId?: string;
  name: string;
  specialty?: string;
  isActive: boolean;
}
