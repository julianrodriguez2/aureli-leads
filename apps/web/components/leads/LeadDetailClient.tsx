"use client";

import Link from "next/link";
import { useState } from "react";
import { toast } from "sonner";
import { ApiError, apiFetch } from "@/lib/api";
import { updateLeadStatus } from "@/lib/auth";
import type { ActivityDto, LeadDetailDto } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";

type LeadDetailClientProps = {
  leadId: string;
  initialLead: LeadDetailDto;
  initialActivities: ActivityDto[];
  initialActivityError?: string | null;
};

const statusOptions = ["New", "Contacted", "Qualified", "Disqualified"] as const;

const statusStyles: Record<string, string> = {
  New: "bg-sky-100 text-sky-700",
  Contacted: "bg-amber-100 text-amber-700",
  Qualified: "bg-emerald-100 text-emerald-700",
  Disqualified: "bg-slate-200 text-slate-600"
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

export function LeadDetailClient({
  leadId,
  initialLead,
  initialActivities,
  initialActivityError
}: LeadDetailClientProps) {
  const [lead, setLead] = useState(initialLead);
  const [activities, setActivities] = useState(initialActivities);
  const [activityError, setActivityError] = useState<string | null>(initialActivityError ?? null);
  const [selectedStatus, setSelectedStatus] = useState(initialLead.status);
  const [isSaving, setIsSaving] = useState(false);

  const metadataJson = lead.metadata ? JSON.stringify(lead.metadata, null, 2) : null;
  const isStatusUnchanged = selectedStatus === lead.status;

  async function refreshData() {
    const [leadResult, activityResult] = await Promise.allSettled([
      apiFetch<LeadDetailDto>(`/api/leads/${leadId}`),
      apiFetch<ActivityDto[]>(`/api/leads/${leadId}/activities`)
    ]);

    if (leadResult.status === "fulfilled") {
      setLead(leadResult.value);
      setSelectedStatus(leadResult.value.status);
    } else {
      toast.error("Unable to refresh lead details.");
    }

    if (activityResult.status === "fulfilled") {
      setActivities(activityResult.value);
      setActivityError(null);
    } else {
      setActivityError("Unable to load activity history.");
    }
  }

  async function handleStatusUpdate() {
    if (isStatusUnchanged) {
      return;
    }

    setIsSaving(true);
    try {
      await updateLeadStatus(leadId, selectedStatus);
      await refreshData();
      toast.success("Status updated.");
    } catch (error) {
      if (error instanceof ApiError && error.status === 403) {
        toast.error("You do not have permission to update status.");
      } else {
        toast.error("Unable to update status.");
      }
    } finally {
      setIsSaving(false);
    }
  }

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
          <span className={`rounded-full px-3 py-1 text-xs font-semibold ${statusStyles[lead.status] ?? "bg-slate-100 text-slate-700"}`}>
            {lead.status}
          </span>
          <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-700">
            Score {lead.score}
          </span>
          <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
            {lead.source}
          </span>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Status update</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap items-end gap-4">
          <div className="space-y-2">
            <Label htmlFor="lead-status">Status</Label>
            <select
              id="lead-status"
              value={selectedStatus}
              onChange={(event) => setSelectedStatus(event.target.value)}
              className="h-9 w-52 rounded-md border border-input bg-background px-3 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            >
              {statusOptions.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>
          <Button onClick={handleStatusUpdate} disabled={isSaving || isStatusUnchanged}>
            {isSaving ? "Updating..." : "Update Status"}
          </Button>
        </CardContent>
      </Card>

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
