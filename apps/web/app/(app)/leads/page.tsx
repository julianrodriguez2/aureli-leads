import { LeadTable } from "@/components/leads/LeadTable";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

export default function LeadsPage() {
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
          <LeadTable />
        </CardContent>
      </Card>
    </div>
  );
}
