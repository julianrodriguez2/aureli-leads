import Link from "next/link";
import { LeadListDto } from "@/lib/types";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

type LeadTableProps = {
  leads: LeadListDto[];
};

export function LeadTable({ leads }: LeadTableProps) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Company</TableHead>
          <TableHead>Status</TableHead>
          <TableHead>Score</TableHead>
          <TableHead className="text-right">Action</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {leads.length === 0 ? (
          <TableRow>
            <TableCell colSpan={5} className="py-10 text-center text-sm text-muted-foreground">
              No leads yet. Import or create your first lead.
            </TableCell>
          </TableRow>
        ) : (
          leads.map((lead) => (
            <TableRow key={lead.id}>
              <TableCell className="font-medium">{lead.firstName} {lead.lastName}</TableCell>
              <TableCell>{lead.company ?? "N/A"}</TableCell>
              <TableCell className="capitalize">{lead.status}</TableCell>
              <TableCell>{lead.score}</TableCell>
              <TableCell className="text-right">
                <Link className="text-sm font-medium text-primary hover:underline" href={`/leads/${lead.id}`}>
                  View
                </Link>
              </TableCell>
            </TableRow>
          ))
        )}
      </TableBody>
    </Table>
  );
}
