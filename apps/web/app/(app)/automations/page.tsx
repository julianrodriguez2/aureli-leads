import Link from "next/link";
import { cookies } from "next/headers";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { apiFetch } from "@/lib/api";
import { AUTH_COOKIE_NAME } from "@/lib/auth";
import type { AutomationEventDto, PagedResponse } from "@/lib/types";

type AutomationsPageProps = {
  searchParams: Record<string, string | string[] | undefined>;
};

const statusOptions = ["Pending", "Sent", "Failed"];
const eventTypeOptions = ["LeadCreated", "LeadScored", "StatusChanged"];

const statusStyles: Record<string, string> = {
  Pending: "bg-amber-100 text-amber-700",
  Sent: "bg-emerald-100 text-emerald-700",
  Failed: "bg-rose-100 text-rose-700"
};

function getSearchParam(searchParams: AutomationsPageProps["searchParams"], key: string) {
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

function isGuid(value: string) {
  return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$/.test(value);
}

function formatDateTime(value?: string | null) {
  if (!value) {
    return "N/A";
  }

  return new Date(value).toLocaleString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit"
  });
}

function truncate(value?: string | null, length = 60) {
  if (!value) {
    return "—";
  }

  return value.length > length ? `${value.slice(0, length)}...` : value;
}

export default async function AutomationsPage({ searchParams }: AutomationsPageProps) {
  const status = getSearchParam(searchParams, "status") ?? "";
  const eventType = getSearchParam(searchParams, "eventType") ?? "";
  const leadIdParam = getSearchParam(searchParams, "leadId") ?? "";
  const pageParam = getSearchParam(searchParams, "page") ?? "1";
  const pageSizeParam = getSearchParam(searchParams, "pageSize") ?? "20";
  const sort = getSearchParam(searchParams, "sort") ?? "createdAt_desc";

  const page = Number.isNaN(Number(pageParam)) ? 1 : Math.max(1, Number(pageParam));
  const pageSize = Number.isNaN(Number(pageSizeParam)) ? 20 : Math.max(1, Number(pageSizeParam));
  const leadId = leadIdParam && isGuid(leadIdParam) ? leadIdParam : "";

  const token = cookies().get(AUTH_COOKIE_NAME)?.value;
  let data: PagedResponse<AutomationEventDto> | null = null;
  let errorMessage: string | null = null;

  try {
    const queryString = buildQueryString({
      status,
      eventType,
      leadId,
      page,
      pageSize,
      sort
    });

    data = await apiFetch<PagedResponse<AutomationEventDto>>(`/api/automation-events?${queryString}`, {
      headers: token ? { Cookie: `${AUTH_COOKIE_NAME}=${token}` } : undefined
    });
  } catch {
    errorMessage = "Unable to load automation events right now.";
  }

  const events = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;
  const totalItems = data?.totalItems ?? 0;

  const prevPageHref = `/automations?${buildQueryString({
    status,
    eventType,
    leadId,
    page: Math.max(1, page - 1),
    pageSize,
    sort
  })}`;

  const nextPageHref = `/automations?${buildQueryString({
    status,
    eventType,
    leadId,
    page: Math.min(totalPages, page + 1),
    pageSize,
    sort
  })}`;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Automation</p>
          <h2 className="text-2xl font-semibold">Automation events</h2>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
        </CardHeader>
        <CardContent>
          <form method="get" className="grid gap-4 md:grid-cols-[repeat(3,1fr)_auto]">
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
              <label className="text-xs uppercase tracking-[0.18em] text-muted-foreground" htmlFor="eventType">Event type</label>
              <select
                id="eventType"
                name="eventType"
                defaultValue={eventType}
                className="h-10 w-full rounded-md border border-border/70 bg-white/80 px-3 text-sm"
              >
                <option value="">All</option>
                {eventTypeOptions.map((option) => (
                  <option key={option} value={option}>{option}</option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-xs uppercase tracking-[0.18em] text-muted-foreground" htmlFor="leadId">Lead ID</label>
              <input
                id="leadId"
                name="leadId"
                placeholder="Optional"
                defaultValue={leadIdParam}
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
          <CardTitle>Event log</CardTitle>
        </CardHeader>
        <CardContent>
          {errorMessage ? (
            <div className="rounded-xl border border-dashed border-border/70 bg-white/80 p-6 text-sm text-muted-foreground">
              {errorMessage}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Event Type</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Lead</TableHead>
                  <TableHead>Attempts</TableHead>
                  <TableHead>Last Attempt</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead>Last Error</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {events.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="py-10 text-center text-sm text-muted-foreground">
                      No automation events yet.
                    </TableCell>
                  </TableRow>
                ) : (
                  events.map((evt) => (
                    <TableRow key={evt.id}>
                      <TableCell className="font-medium">{evt.eventType}</TableCell>
                      <TableCell>
                        <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${statusStyles[evt.status] ?? "bg-slate-100 text-slate-700"}`}>
                          {evt.status}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Link href={`/leads/${evt.leadId}`} className="text-sm text-primary underline-offset-4 hover:underline">
                          {evt.leadId.slice(0, 8)}
                        </Link>
                      </TableCell>
                      <TableCell>{evt.attemptCount}</TableCell>
                      <TableCell>{formatDateTime(evt.lastAttemptAt)}</TableCell>
                      <TableCell>{formatDateTime(evt.createdAt)}</TableCell>
                      <TableCell className="text-xs text-muted-foreground">{truncate(evt.lastError)}</TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
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
