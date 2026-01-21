import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export default function AutomationsPage() {
  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Automation</p>
          <h2 className="text-2xl font-semibold">Automations</h2>
        </div>
        <Button>Create automation</Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Queue status</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Event</TableHead>
                <TableHead>Lead</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Scheduled</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              <TableRow>
                <TableCell className="font-medium">Lead nurture email</TableCell>
                <TableCell>Helios Health</TableCell>
                <TableCell>Queued</TableCell>
                <TableCell>Today, 3:00 PM</TableCell>
              </TableRow>
              <TableRow>
                <TableCell className="font-medium">Score recalculation</TableCell>
                <TableCell>Nova Labs</TableCell>
                <TableCell>Processing</TableCell>
                <TableCell>Today, 2:15 PM</TableCell>
              </TableRow>
            </TableBody>
          </Table>
          {/* TODO: fetch automation events from API. */}
        </CardContent>
      </Card>
    </div>
  );
}
