import Link from "next/link";

const navItems = [
  { href: "/leads", label: "Leads" },
  { href: "/automations", label: "Automations" },
  { href: "/settings", label: "Settings" }
];

export function AppSidebar() {
  return (
    <aside className="w-56 shrink-0">
      <nav className="glass-panel space-y-1 p-4">
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-muted-foreground">Workspace</p>
        <div className="mt-4 flex flex-col gap-2">
          {navItems.map((item) => (
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
