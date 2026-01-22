import { LeadListItemDto } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

type LeadTableProps = {
  leads: LeadListItemDto[];
};

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

export function LeadTable({ leads }: LeadTableProps) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Lead</TableHead>
          <TableHead>Source</TableHead>
          <TableHead>Status</TableHead>
          <TableHead>Score</TableHead>
          <TableHead>Created</TableHead>
          <TableHead>Last Activity</TableHead>
          <TableHead className="text-right">Action</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {leads.length === 0 ? (
          <TableRow>
            <TableCell colSpan={7} className="py-10 text-center text-sm text-muted-foreground">
              No leads yet. Import or create your first lead.
            </TableCell>
          </TableRow>
        ) : (
          leads.map((lead) => (
            <TableRow key={lead.id}>
              <TableCell>
                <div className="font-medium">{lead.firstName} {lead.lastName}</div>
                <div className="text-xs text-muted-foreground">{lead.email}</div>
                <div className="text-xs text-muted-foreground">{lead.phone ?? "No phone"}</div>
              </TableCell>
              <TableCell className="uppercase text-xs tracking-[0.12em] text-muted-foreground">{lead.source}</TableCell>
              <TableCell>
                <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${statusStyles[lead.status] ?? "bg-slate-100 text-slate-600"}`}>
                  {lead.status}
                </span>
              </TableCell>
              <TableCell>{lead.score}</TableCell>
              <TableCell>{formatDate(lead.createdAt)}</TableCell>
              <TableCell>{formatDate(lead.lastActivityAt ?? lead.createdAt)}</TableCell>
              <TableCell className="text-right">
                <Button variant="ghost" size="sm" disabled>
                  View
                </Button>
              </TableCell>
            </TableRow>
          ))
        )}
      </TableBody>
    </Table>
  );
}
