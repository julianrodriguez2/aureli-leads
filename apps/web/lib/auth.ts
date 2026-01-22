import { apiFetch } from "@/lib/api";
import type { AuthResponseDto, MeDto } from "@/lib/types";

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
