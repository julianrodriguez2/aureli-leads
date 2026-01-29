import { apiFetch } from "@/lib/api";
import type {
  ActivityDto,
  AuthResponseDto,
  LeadDetailDto,
  MeDto,
  SettingsDto,
  UpdateSettingsRequest,
  WebhookTestResponse
} from "@/lib/types";

export const AUTH_COOKIE_NAME = "access_token";

type LoginRequest = {
  email: string;
  password: string;
};

export async function login(payload: LoginRequest): Promise<MeDto> {
  const response = await apiFetch<AuthResponseDto>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify(payload)
  });

  return response.user;
}

export async function logout(): Promise<void> {
  await apiFetch("/api/auth/logout", {
    method: "POST"
  });
}

export async function me(cookie?: string): Promise<MeDto> {
  return apiFetch<MeDto>("/api/auth/me", {
    headers: cookie ? { Cookie: cookie } : undefined
  });
}

export async function updateLeadStatus(id: string, status: string): Promise<LeadDetailDto> {
  return apiFetch<LeadDetailDto>(`/api/leads/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status })
  });
}

export async function rescoreLead(id: string): Promise<LeadDetailDto> {
  return apiFetch<LeadDetailDto>(`/api/leads/${id}/score`, {
    method: "POST"
  });
}

export async function retryAutomationEvent(id: string): Promise<void> {
  await apiFetch(`/api/automation-events/${id}/retry`, {
    method: "POST"
  });
}

export async function addLeadNote(leadId: string, text: string): Promise<ActivityDto> {
  return apiFetch<ActivityDto>(`/api/leads/${leadId}/notes`, {
    method: "POST",
    body: JSON.stringify({ text })
  });
}

export async function getSettings(cookie?: string): Promise<SettingsDto> {
  return apiFetch<SettingsDto>("/api/settings", {
    headers: cookie ? { Cookie: cookie } : undefined
  });
}

export async function updateWebhookSettings(payload: UpdateSettingsRequest): Promise<SettingsDto> {
  return apiFetch<SettingsDto>("/api/settings/webhook", {
    method: "PATCH",
    body: JSON.stringify(payload)
  });
}

export async function testWebhook(): Promise<WebhookTestResponse> {
  return apiFetch<WebhookTestResponse>("/api/settings/webhook/test", {
    method: "POST"
  });
}
