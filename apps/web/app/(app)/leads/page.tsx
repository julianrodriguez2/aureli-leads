import { cookies } from "next/headers";
import { LeadTable } from "@/components/leads/LeadTable";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { apiFetch } from "@/lib/api";
import { AUTH_COOKIE_NAME } from "@/lib/auth";
import { LeadListDto } from "@/lib/types";

export default async function LeadsPage() {
  const token = cookies().get(AUTH_COOKIE_NAME)?.value;
  const leads = await apiFetch<LeadListDto[]>("/api/leads", {
    headers: token ? { Cookie: `${AUTH_COOKIE_NAME}=${token}` } : undefined
  });

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
          <CardTitle>Active leads</CardTitle>
        </CardHeader>
        <CardContent>
          <LeadTable leads={leads} />
        </CardContent>
      </Card>
    </div>
  );
}
