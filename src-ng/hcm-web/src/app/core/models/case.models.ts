export interface CaseStatusHistoryDto {
  id: string;
  fromStatus?: string;
  toStatus: string;
  comment?: string;
  changedBy: string;
  changedByUserName: string;
  changedAt: string;
}

export interface CareTeamMemberDto {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  teamRole: string;
  joinedAt: string;
  leftAt?: string;
  isActive: boolean;
}

export interface CaseNoteDto {
  id: string;
  caseId: string;
  createdBy: string;
  createdByUserName: string;
  content: string;
  isEditable: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CaseDto {
  id: string;
  caseNumber: string;
  patientId: string;
  caseTypeId: number;
  caseTypeName: string;
  currentStatus: string;
  openedAt?: string;
  closedAt?: string;
  assignedToUserId?: string;
  assignedToUserName?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
  statusHistory: CaseStatusHistoryDto[];
  careTeam: CareTeamMemberDto[];
  notes_Collection: CaseNoteDto[];
}

export interface CaseListItemDto {
  id: string;
  caseNumber: string;
  caseTypeId: number;
  caseTypeName: string;
  currentStatus: string;
  openedAt?: string;
  closedAt?: string;
  assignedToUserName?: string;
}

export interface CaseListResult {
  totalCount: number;
  page: number;
  pageSize: number;
  items: CaseListItemDto[];
}

export interface CreateCaseRequest {
  patientId: string;
  caseTypeId: number;
  notes?: string;
}

export interface UpdateCaseRequest {
  caseTypeId?: number;
  assignedToUserId?: string;
  notes?: string;
}

export interface CaseStatusChangeRequest {
  newStatus: string;
  comment?: string;
}

export interface AddCareTeamMemberRequest {
  userId: string;
  teamRole: string;
}

export interface CreateCaseNoteRequest {
  content: string;
}

export interface CaseTypeDto {
  id: number;
  name: string;
  isActive: boolean;
}
