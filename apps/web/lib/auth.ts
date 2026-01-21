import { cookies } from "next/headers";
import { redirect } from "next/navigation";

export const AUTH_COOKIE_NAME = "aureli_auth";

export function requireAuth() {
  const token = cookies().get(AUTH_COOKIE_NAME)?.value;
  if (!token) {
    redirect("/login");
  }
  return token;
}

export function getAuthToken() {
  return cookies().get(AUTH_COOKIE_NAME)?.value ?? null;
}
