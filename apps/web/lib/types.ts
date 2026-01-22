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

export type LeadActivityDto = {
  id: string;
  leadId: string;
  type: string;
  notes?: string | null;
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

export type LeadDetailDto = {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  company?: string | null;
  status: string;
  score: number;
  createdAt: string;
  updatedAt: string;
  activities: LeadActivityDto[];
  automationEvents: AutomationEventDetailDto[];
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
