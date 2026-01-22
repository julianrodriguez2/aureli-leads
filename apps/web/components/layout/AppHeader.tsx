import Link from "next/link";
import { Button } from "@/components/ui/button";
import { LogoutButton } from "@/components/layout/LogoutButton";
import type { MeDto } from "@/lib/types";

type AppHeaderProps = {
  user: MeDto;
};

export function AppHeader({ user }: AppHeaderProps) {
  return (
    <header className="border-b border-border/60 bg-white/70 backdrop-blur">
      <div className="mx-auto flex w-full max-w-6xl items-center justify-between px-4 py-4">
        <Link href="/leads" className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-amber-500 to-sky-500" />
          <div>
            <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Aureli</p>
            <p className="text-lg font-semibold">Lead Studio</p>
          </div>
        </Link>
        <div className="flex items-center gap-4">
          <div className="text-right">
            <p className="text-sm font-medium text-foreground">{user.email}</p>
            <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">{user.role}</p>
          </div>
          <Button variant="secondary" size="sm">New lead</Button>
          <LogoutButton />
        </div>
      </div>
    </header>
  );
}
