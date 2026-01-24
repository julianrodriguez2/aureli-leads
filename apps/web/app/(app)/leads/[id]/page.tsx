import Link from "next/link";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import { AUTH_COOKIE_NAME } from "@/lib/auth";
import type { ActivityDto, LeadDetailDto } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

type LeadDetailPageProps = {
  params: { id: string };
};

function formatDate(value?: string | null) {
  if (!value) {
    return "N/A";
  }

  return new Date(value).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric"
  });
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

export default async function LeadDetailPage({ params }: LeadDetailPageProps) {
  const token = cookies().get(AUTH_COOKIE_NAME)?.value;
  if (!token) {
    redirect("/login");
  }

  let lead: LeadDetailDto | null = null;
  let activities: ActivityDto[] = [];
  let errorMessage: string | null = null;
  let activityError: string | null = null;
  let notFound = false;

  try {
    lead = await apiFetch<LeadDetailDto>(`/api/leads/${params.id}`, {
      headers: { Cookie: `${AUTH_COOKIE_NAME}=${token}` }
    });
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 401) {
        redirect("/login");
      }
      if (error.status === 404) {
        notFound = true;
      } else {
        errorMessage = "Unable to load this lead.";
      }
    } else {
      errorMessage = "Unable to load this lead.";
    }
  }

  if (notFound) {
    return (
      <div className="space-y-4">
        <Button variant="ghost" size="sm" asChild>
          <Link href="/leads">Back to Leads</Link>
        </Button>
        <div className="rounded-2xl border border-dashed border-border/70 bg-white/80 p-6">
          <h2 className="text-lg font-semibold">Lead not found</h2>
          <p className="text-sm text-muted-foreground">The lead you are looking for does not exist.</p>
        </div>
      </div>
    );
  }

  if (!lead) {
    return (
      <div className="space-y-4">
        <Button variant="ghost" size="sm" asChild>
          <Link href="/leads">Back to Leads</Link>
        </Button>
        <div className="rounded-2xl border border-dashed border-border/70 bg-white/80 p-6 text-sm text-muted-foreground">
          {errorMessage ?? "Unable to load this lead."}
        </div>
      </div>
    );
  }

  try {
    activities = await apiFetch<ActivityDto[]>(`/api/leads/${params.id}/activities`, {
      headers: { Cookie: `${AUTH_COOKIE_NAME}=${token}` }
    });
  } catch {
    activityError = "Unable to load activity history.";
  }

  const metadataJson = lead.metadata ? JSON.stringify(lead.metadata, null, 2) : null;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-2">
          <Button variant="ghost" size="sm" asChild>
            <Link href="/leads">Back to Leads</Link>
          </Button>
          <div>
            <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Lead profile</p>
            <h2 className="text-2xl font-semibold">{lead.firstName} {lead.lastName}</h2>
            <p className="text-sm text-muted-foreground">Created {formatDate(lead.createdAt)}</p>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700">{lead.status}</span>
          <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-700">Score {lead.score}</span>
          <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">{lead.source}</span>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1.2fr_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Lead info</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm text-muted-foreground">
            <p><span className="font-medium text-foreground">Email:</span> {lead.email}</p>
            <p><span className="font-medium text-foreground">Phone:</span> {lead.phone ?? "N/A"}</p>
            <p><span className="font-medium text-foreground">Created:</span> {formatDate(lead.createdAt)}</p>
            <p><span className="font-medium text-foreground">Message:</span> {lead.message ?? "No message provided."}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Tags and metadata</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-sm text-muted-foreground">
            <div className="flex flex-wrap gap-2">
              {lead.tags.length === 0 ? (
                <span className="text-xs">No tags yet.</span>
              ) : (
                lead.tags.map((tag) => (
                  <span key={tag} className="rounded-full bg-secondary px-3 py-1 text-xs font-semibold text-secondary-foreground">
                    {tag}
                  </span>
                ))
              )}
            </div>
            <details className="rounded-xl border border-border/60 bg-white/80 p-4">
              <summary className="cursor-pointer text-sm font-medium text-foreground">Metadata</summary>
              <pre className="mt-3 whitespace-pre-wrap text-xs text-muted-foreground">
                {metadataJson ?? "No metadata yet."}
              </pre>
            </details>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Score breakdown</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm text-muted-foreground">
          {lead.scoreReasons.length === 0 ? (
            <p>No score breakdown yet.</p>
          ) : (
            lead.scoreReasons.map((reason, index) => (
              <div key={`${reason.rule}-${reason.delta}-${index}`} className="flex items-center justify-between rounded-lg border border-border/60 bg-white/70 px-4 py-2">
                <span className="font-medium text-foreground">{reason.rule}</span>
                <span className="text-sm">{reason.delta > 0 ? `+${reason.delta}` : reason.delta}</span>
              </div>
            ))
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Activity timeline</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {activityError ? (
            <p className="text-sm text-muted-foreground">{activityError}</p>
          ) : activities.length === 0 ? (
            <p className="text-sm text-muted-foreground">No activity yet.</p>
          ) : (
            activities.map((activity) => (
              <div key={activity.id} className="rounded-xl border border-border/60 bg-white/70 p-4">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <p className="text-sm font-semibold text-foreground">{activity.type}</p>
                  <p className="text-xs text-muted-foreground">{formatDateTime(activity.createdAt)}</p>
                </div>
                {activity.data ? (
                  <pre className="mt-2 whitespace-pre-wrap text-xs text-muted-foreground">
                    {JSON.stringify(activity.data, null, 2)}
                  </pre>
                ) : (
                  <p className="mt-2 text-xs text-muted-foreground">No data payload.</p>
                )}
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
