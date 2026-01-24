export default function LeadDetailLoading() {
  return (
    <div className="space-y-6 animate-pulse">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-2">
          <div className="h-8 w-40 rounded bg-muted" />
          <div className="h-5 w-56 rounded bg-muted" />
        </div>
        <div className="flex gap-2">
          <div className="h-6 w-20 rounded-full bg-muted" />
          <div className="h-6 w-20 rounded-full bg-muted" />
          <div className="h-6 w-20 rounded-full bg-muted" />
        </div>
      </div>
      <div className="grid gap-6 lg:grid-cols-[1.2fr_1fr]">
        <div className="rounded-2xl border border-border/60 bg-white/70 p-6">
          <div className="h-4 w-24 rounded bg-muted" />
          <div className="mt-4 space-y-3">
            {Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="h-4 w-full rounded bg-muted" />
            ))}
          </div>
        </div>
        <div className="rounded-2xl border border-border/60 bg-white/70 p-6">
          <div className="h-4 w-32 rounded bg-muted" />
          <div className="mt-4 space-y-3">
            {Array.from({ length: 3 }).map((_, index) => (
              <div key={index} className="h-4 w-full rounded bg-muted" />
            ))}
          </div>
        </div>
      </div>
      <div className="rounded-2xl border border-border/60 bg-white/70 p-6">
        <div className="h-4 w-28 rounded bg-muted" />
        <div className="mt-4 space-y-3">
          {Array.from({ length: 5 }).map((_, index) => (
            <div key={index} className="h-8 w-full rounded bg-muted" />
          ))}
        </div>
      </div>
    </div>
  );
}
