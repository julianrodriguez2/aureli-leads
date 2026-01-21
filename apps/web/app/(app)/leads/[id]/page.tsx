import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

type LeadDetailPageProps = {
  params: { id: string };
};

export default function LeadDetailPage({ params }: LeadDetailPageProps) {
  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Lead profile</p>
          <h2 className="text-2xl font-semibold">Lead {params.id}</h2>
        </div>
        <Button variant="secondary">Trigger automation</Button>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1.2fr_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Overview</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-sm text-muted-foreground">
            <p><span className="font-medium text-foreground">Name:</span> Ava Chen</p>
            <p><span className="font-medium text-foreground">Company:</span> Helios Health</p>
            <p><span className="font-medium text-foreground">Email:</span> ava.chen@example.com</p>
            <p><span className="font-medium text-foreground">Status:</span> New</p>
            <p><span className="font-medium text-foreground">Score:</span> 72</p>
            {/* TODO: hydrate with API data. */}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Next actions</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm text-muted-foreground">
            <div className="rounded-xl border border-dashed border-border/70 p-3">
              <p className="font-medium text-foreground">Schedule call</p>
              <p>Send a Calendly invite.</p>
            </div>
            <div className="rounded-xl border border-dashed border-border/70 p-3">
              <p className="font-medium text-foreground">Score refresh</p>
              <p>Recalculate engagement score.</p>
            </div>
            {/* TODO: load automation suggestions. */}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Recent activity</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Type</TableHead>
                <TableHead>Details</TableHead>
                <TableHead>Date</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              <TableRow>
                <TableCell className="font-medium">Email opened</TableCell>
                <TableCell>Sequence A - touch 2</TableCell>
                <TableCell>Jan 20, 2025</TableCell>
              </TableRow>
              <TableRow>
                <TableCell className="font-medium">Form submitted</TableCell>
                <TableCell>Demo request</TableCell>
                <TableCell>Jan 18, 2025</TableCell>
              </TableRow>
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
