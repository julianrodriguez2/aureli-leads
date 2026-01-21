import { AppHeader } from "@/components/layout/AppHeader";
import { AppSidebar } from "@/components/layout/AppSidebar";
import { requireAuth } from "@/lib/auth";

export default function AppLayout({
  children
}: {
  children: React.ReactNode;
}) {
  requireAuth();

  return (
    <div className="min-h-screen">
      <AppHeader />
      <div className="mx-auto flex w-full max-w-6xl gap-6 px-4 pb-12 pt-6">
        <AppSidebar />
        <main className="min-h-[70vh] flex-1 animate-fade-up">
          {children}
        </main>
      </div>
    </div>
  );
}
