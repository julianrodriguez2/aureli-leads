import Link from "next/link";
import { cookies } from "next/headers";
import { LeadTable } from "@/components/leads/LeadTable";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiError, apiFetch } from "@/lib/api";
import { AUTH_COOKIE_NAME } from "@/lib/auth";
import type { LeadListItemDto, PagedResponse } from "@/lib/types";

type LeadsPageProps = {
  searchParams: Record<string, string | string[] | undefined>;
};

const statusOptions = ["New", "Contacted", "Qualified", "Disqualified"];
const sourceOptions = ["web", "google_ads", "referral", "n8n"];

function getSearchParam(searchParams: LeadsPageProps["searchParams"], key: string) {
  const value = searchParams[key];
  return Array.isArray(value) ? value[0] : value;
}

function buildQueryString(params: Record<string, string | number | undefined | null>) {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "") {
      return;
    }
    query.set(key, String(value));
  });
  return query.toString();
}

export default async function LeadsPage({ searchParams }: LeadsPageProps) {
  const q = getSearchParam(searchParams, "q") ?? "";
  const status = getSearchParam(searchParams, "status") ?? "";
  const source = getSearchParam(searchParams, "source") ?? "";
  const minScoreParam = getSearchParam(searchParams, "minScore") ?? "";
  const pageParam = getSearchParam(searchParams, "page") ?? "1";
  const pageSizeParam = getSearchParam(searchParams, "pageSize") ?? "20";
  const sort = getSearchParam(searchParams, "sort") ?? "createdAt_desc";

  const minScoreValue = minScoreParam.trim();
  const parsedMinScore = minScoreValue === "" ? undefined : Number(minScoreValue);
  const minScore = Number.isNaN(parsedMinScore) ? undefined : parsedMinScore;
  const page = Number.isNaN(Number(pageParam)) ? 1 : Math.max(1, Number(pageParam));
  const pageSize = Number.isNaN(Number(pageSizeParam)) ? 20 : Math.max(1, Number(pageSizeParam));

  const token = cookies().get(AUTH_COOKIE_NAME)?.value;
  let data: PagedResponse<LeadListItemDto> | null = null;
  let errorMessage: string | null = null;
  let errorTraceId: string | null = null;

  try {
    const queryString = buildQueryString({
      q,
      status,
      source,
      minScore,
      page,
      pageSize,
      sort
    });

    data = await apiFetch<PagedResponse<LeadListItemDto>>(`/api/leads?${queryString}`, {
      headers: token ? { Cookie: `${AUTH_COOKIE_NAME}=${token}` } : undefined
    });
  } catch (error) {
    if (error instanceof ApiError) {
      errorMessage = error.message || "Unable to load leads right now.";
      errorTraceId = error.traceId ?? null;
    } else {
      errorMessage = "Unable to load leads right now.";
    }
  }

  const leads = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;
  const totalItems = data?.totalItems ?? 0;
  const hasFilters = Boolean(q || status || source || minScoreValue);
  const emptyMessage = hasFilters
    ? "No leads match the current filters."
    : "No leads yet. Import or create your first lead.";

  const prevPageHref = `/leads?${buildQueryString({
    q,
    status,
    source,
    minScore,
    page: Math.max(1, page - 1),
    pageSize,
    sort
  })}`;

  const nextPageHref = `/leads?${buildQueryString({
    q,
    status,
    source,
    minScore,
    page: Math.min(totalPages, page + 1),
    pageSize,
    sort
  })}`;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Pipeline</p>
          <h2 className="text-2xl font-semibold">Leads</h2>
        </div>
        <Button>Import leads</Button>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
        </CardHeader>
        <CardContent>
          <form method="get" className="grid gap-4 md:grid-cols-[1.4fr_repeat(3,1fr)_auto]">
            <div className="space-y-2">
              <label className="text-xs uppercase tracking-[0.18em] text-muted-foreground" htmlFor="q">Search</label>
              <input
                id="q"
                name="q"
                defaultValue={q}
                placeholder="Name, email, phone, or message"
                className="h-10 w-full rounded-md border border-border/70 bg-white/80 px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              />
            </div>
            <div className="space-y-2">
              <label className="text-xs uppercase tracking-[0.18em] text-muted-foreground" htmlFor="status">Status</label>
              <select
                id="status"
                name="status"
                defaultValue={status}
                className="h-10 w-full rounded-md border border-border/70 bg-white/80 px-3 text-sm"
              >
                <option value="">All</option>
                {statusOptions.map((option) => (
                  <option key={option} value={option}>{option}</option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-xs uppercase tracking-[0.18em] text-muted-foreground" htmlFor="source">Source</label>
              <select
                id="source"
                name="source"
                defaultValue={source}
                className="h-10 w-full rounded-md border border-border/70 bg-white/80 px-3 text-sm"
              >
                <option value="">All</option>
                {sourceOptions.map((option) => (
                  <option key={option} value={option}>{option}</option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-xs uppercase tracking-[0.18em] text-muted-foreground" htmlFor="minScore">Min score</label>
              <input
                id="minScore"
                name="minScore"
                type="number"
                min={0}
                max={100}
                defaultValue={minScoreValue}
                className="h-10 w-full rounded-md border border-border/70 bg-white/80 px-3 text-sm"
              />
            </div>
            <div className="flex items-end">
              <input type="hidden" name="page" value="1" />
              <input type="hidden" name="pageSize" value={pageSize} />
              <input type="hidden" name="sort" value={sort} />
              <Button type="submit">Apply</Button>
            </div>
          </form>
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Active leads</CardTitle>
        </CardHeader>
        <CardContent>
          {errorMessage ? (
            <div className="rounded-xl border border-dashed border-border/70 bg-white/80 p-6 text-sm text-muted-foreground">
              <p>{errorMessage}</p>
              {errorTraceId ? (
                <p className="mt-2 text-xs text-muted-foreground">Trace ID: {errorTraceId}</p>
              ) : null}
            </div>
          ) : (
            <LeadTable leads={leads} emptyMessage={emptyMessage} />
          )}
        </CardContent>
      </Card>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-muted-foreground">
          Page {data?.page ?? page} of {totalPages} - {totalItems} total
        </p>
        <div className="flex items-center gap-2">
          {page <= 1 ? (
            <Button variant="outline" size="sm" disabled>Prev</Button>
          ) : (
            <Button variant="outline" size="sm" asChild>
              <Link href={prevPageHref}>Prev</Link>
            </Button>
          )}
          {page >= totalPages ? (
            <Button variant="outline" size="sm" disabled>Next</Button>
          ) : (
            <Button variant="outline" size="sm" asChild>
              <Link href={nextPageHref}>Next</Link>
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
