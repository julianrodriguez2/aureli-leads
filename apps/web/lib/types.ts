export type LeadListDto = {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  company?: string | null;
  status: string;
  score: number;
  createdAt: string;
};

export type LeadListItemDto = {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  source: string;
  status: string;
  score: number;
  createdAt: string;
  lastActivityAt?: string | null;
  automationStatus: string;
};

export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type LeadActivityDto = {
  id: string;
  leadId: string;
  type: string;
  notes?: string | null;
  createdAt: string;
};

export type ScoreReasonDto = {
  rule: string;
  delta: number;
};

export type ActivityDto = {
  id: string;
  type: string;
  data?: Record<string, unknown> | null;
  createdAt: string;
};

export type AutomationEventDetailDto = {
  id: string;
  leadId: string;
  eventType: string;
  payload?: string | null;
  status: string;
  scheduledAt: string;
  processedAt?: string | null;
  createdAt: string;
};

export type AutomationEventDto = {
  id: string;
  leadId: string;
  eventType: string;
  status: string;
  attemptCount: number;
  lastAttemptAt?: string | null;
  lastError?: string | null;
  createdAt: string;
};

export type LeadDetailDto = {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  source: string;
  status: string;
  score: number;
  scoreReasons: ScoreReasonDto[];
  message?: string | null;
  tags: string[];
  metadata?: Record<string, unknown> | null;
  createdAt: string;
  updatedAt: string;
};

export type AutomationEventListDto = {
  id: string;
  leadId: string;
  eventType: string;
  status: string;
  scheduledAt: string;
  createdAt: string;
};

export type SettingDto = {
  id: string;
  key: string;
  value: string;
  description?: string | null;
  updatedAt: string;
};

export type SettingsDto = {
  webhookTargetUrl?: string | null;
  webhookSecret?: string | null;
  hasWebhookSecret?: boolean;
};

export type UpdateSettingsRequest = {
  webhookTargetUrl: string;
  webhookSecret?: string;
  rotateSecret?: boolean;
};

export type WebhookTestResponse = {
  ok: boolean;
  statusCode: number;
  error?: string | null;
};

export type UserDto = {
  id: string;
  email: string;
  role: string;
  isActive: boolean;
  lastLoginAt?: string | null;
};

export type MeDto = UserDto;

export type AuthResponseDto = {
  user: UserDto;
  tokenType: string;
  expiresInMinutes: number;
};
