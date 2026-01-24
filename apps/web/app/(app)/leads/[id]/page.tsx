import Link from "next/link";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import { AUTH_COOKIE_NAME } from "@/lib/auth";
import type { ActivityDto, LeadDetailDto } from "@/lib/types";
import { LeadDetailClient } from "@/components/leads/LeadDetailClient";
import { Button } from "@/components/ui/button";

type LeadDetailPageProps = {
  params: { id: string };
};

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

  return (
    <LeadDetailClient
      leadId={params.id}
      initialLead={lead}
      initialActivities={activities}
      initialActivityError={activityError}
    />
  );
}
