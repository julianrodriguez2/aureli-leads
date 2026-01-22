import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { AppHeader } from "@/components/layout/AppHeader";
import { AppSidebar } from "@/components/layout/AppSidebar";
import { AUTH_COOKIE_NAME, me } from "@/lib/auth";
import type { MeDto } from "@/lib/types";

export default async function AppLayout({
  children
}: {
  children: React.ReactNode;
}) {
  const token = cookies().get(AUTH_COOKIE_NAME)?.value;
  if (!token) {
    redirect("/login");
  }

  let user: MeDto;

  try {
    user = await me(`${AUTH_COOKIE_NAME}=${token}`);
  } catch {
    redirect("/login");
  }

  return (
    <div className="min-h-screen">
      <AppHeader user={user} />
      <div className="mx-auto flex w-full max-w-6xl gap-6 px-4 pb-12 pt-6">
        <AppSidebar />
        <main className="min-h-[70vh] flex-1 animate-fade-up">
          {children}
        </main>
      </div>
    </div>
  );
}
