"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { LeadListItemDto } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

type LeadTableProps = {
  leads: LeadListItemDto[];
  emptyMessage?: string;
};

const statusStyles: Record<string, string> = {
  New: "bg-sky-100 text-sky-700",
  Contacted: "bg-amber-100 text-amber-700",
  Qualified: "bg-emerald-100 text-emerald-700",
  Disqualified: "bg-slate-200 text-slate-600"
};

const badgeBase = "inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold";

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

export function LeadTable({ leads, emptyMessage }: LeadTableProps) {
  const router = useRouter();

  function handleRowClick(leadId: string) {
    router.push(`/leads/${leadId}`);
  }

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
              {emptyMessage ?? "No leads yet. Import or create your first lead."}
            </TableCell>
          </TableRow>
        ) : (
          leads.map((lead) => (
            <TableRow
              key={lead.id}
              role="button"
              tabIndex={0}
              onClick={() => handleRowClick(lead.id)}
              onKeyDown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                  event.preventDefault();
                  handleRowClick(lead.id);
                }
              }}
              className="cursor-pointer transition hover:bg-secondary/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <TableCell>
                <div className="font-medium">{lead.firstName} {lead.lastName}</div>
                <div className="text-xs text-muted-foreground">{lead.email}</div>
                <div className="text-xs text-muted-foreground">{lead.phone ?? "No phone"}</div>
              </TableCell>
              <TableCell className="uppercase text-xs tracking-[0.12em] text-muted-foreground">{lead.source}</TableCell>
              <TableCell>
                <span className={`${badgeBase} ${statusStyles[lead.status] ?? "bg-slate-100 text-slate-600"}`}>
                  {lead.status}
                </span>
              </TableCell>
              <TableCell>{lead.score}</TableCell>
              <TableCell>{formatDate(lead.createdAt)}</TableCell>
              <TableCell>{formatDate(lead.lastActivityAt ?? lead.createdAt)}</TableCell>
              <TableCell className="text-right">
                <Button
                  variant="ghost"
                  size="sm"
                  asChild
                  onClick={(event) => event.stopPropagation()}
                >
                  <Link href={`/leads/${lead.id}`}>View</Link>
                </Button>
              </TableCell>
            </TableRow>
          ))
        )}
      </TableBody>
    </Table>
  );
}
