export interface NotificationDto {
  id: number;
  type: string;
  title: string;
  message: string;
  entityType?: string;
  entityId?: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string;
}

export interface NotificationListDto {
  totalCount: number;
  unreadCount: number;
  items: NotificationDto[];
}

export interface UnreadCountDto {
  count: number;
}

export interface AuditLogDto {
  id: number;
  entityType: string;
  entityId: string;
  action: string;
  oldValues?: string;
  newValues?: string;
  changedBy?: string;
  changedByUserName?: string;
  changedAt: string;
}

export interface AuditLogListDto {
  totalCount: number;
  page: number;
  pageSize: number;
  items: AuditLogDto[];
}
