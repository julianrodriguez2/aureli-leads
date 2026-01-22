export default function LeadsLoading() {
  return (
    <div className="space-y-6 animate-pulse">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="h-3 w-24 rounded bg-muted" />
          <div className="mt-2 h-6 w-32 rounded bg-muted" />
        </div>
        <div className="h-9 w-28 rounded bg-muted" />
      </div>
      <div className="rounded-2xl border border-border/60 bg-white/70 p-6">
        <div className="h-4 w-20 rounded bg-muted" />
        <div className="mt-4 grid gap-4 md:grid-cols-[1.4fr_repeat(3,1fr)_auto]">
          {Array.from({ length: 5 }).map((_, index) => (
            <div key={index} className="h-10 rounded bg-muted" />
          ))}
        </div>
      </div>
      <div className="rounded-2xl border border-border/60 bg-white/70 p-6">
        <div className="h-4 w-24 rounded bg-muted" />
        <div className="mt-4 space-y-3">
          {Array.from({ length: 6 }).map((_, index) => (
            <div key={index} className="h-10 rounded bg-muted" />
          ))}
        </div>
      </div>
    </div>
  );
}
