import Link from "next/link";
import type { MeDto } from "@/lib/types";

const navItems = [
  { href: "/leads", label: "Leads" },
  { href: "/automations", label: "Automations" },
  { href: "/settings", label: "Settings" }
];

type AppSidebarProps = {
  user: MeDto;
};

export function AppSidebar({ user }: AppSidebarProps) {
  const isAdmin = user.role.toLowerCase() === "admin";
  const items = isAdmin ? [...navItems, { href: "/users", label: "Users" }] : navItems;

  return (
    <aside className="w-56 shrink-0">
      <nav className="glass-panel space-y-2 p-4">
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-muted-foreground">Workspace</p>
        <div className="mt-4 flex flex-col gap-2">
          {items.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="rounded-lg px-3 py-2 text-sm font-medium text-foreground/80 transition hover:bg-secondary/70"
            >
              {item.label}
            </Link>
          ))}
        </div>
      </nav>
    </aside>
  );
}
