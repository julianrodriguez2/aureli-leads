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

  const isDemo = process.env.NODE_ENV !== "production";

  return (
    <div className="min-h-screen">
      {isDemo ? (
        <div className="border-b border-amber-200/70 bg-amber-50 px-4 py-2 text-center text-xs font-semibold uppercase tracking-[0.2em] text-amber-800">
          Demo Mode — seeded data enabled
        </div>
      ) : null}
      <AppHeader user={user} isApiOnline />
      <div className="mx-auto flex w-full max-w-6xl gap-6 px-4 pb-12 pt-6">
        <AppSidebar user={user} />
        <main className="min-h-[70vh] flex-1 animate-fade-up">
          {children}
        </main>
      </div>
    </div>
  );
}
