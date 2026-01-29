import { cookies } from "next/headers";
import { WebhookSettingsForm } from "@/components/settings/WebhookSettingsForm";
import { AUTH_COOKIE_NAME, getSettings, me } from "@/lib/auth";
import type { MeDto, SettingsDto } from "@/lib/types";

export default async function SettingsPage() {
  const token = cookies().get(AUTH_COOKIE_NAME)?.value;
  let settings: SettingsDto | null = null;
  let errorMessage: string | null = null;
  let user: MeDto | null = null;

  if (token) {
    try {
      user = await me(`${AUTH_COOKIE_NAME}=${token}`);
    } catch {
      user = null;
    }

    try {
      settings = await getSettings(`${AUTH_COOKIE_NAME}=${token}`);
    } catch {
      errorMessage = "Unable to load settings right now.";
    }
  } else {
    errorMessage = "Authentication required.";
  }

  const isAdmin = user?.role?.toLowerCase() === "admin";

  return (
    <div className="space-y-6">
      <div>
        <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Workspace</p>
        <h2 className="text-2xl font-semibold">Settings</h2>
      </div>

      <WebhookSettingsForm
        initialSettings={settings}
        isAdmin={isAdmin}
        errorMessage={errorMessage}
      />
    </div>
  );
}
