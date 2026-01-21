import Link from "next/link";
import { LeadListDto } from "@/lib/types";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

const placeholderLeads: LeadListDto[] = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    firstName: "Ava",
    lastName: "Chen",
    email: "ava.chen@example.com",
    company: "Helios Health",
    status: "new",
    score: 72,
    createdAt: "2025-01-14T10:05:00Z"
  },
  {
    id: "22222222-2222-2222-2222-222222222222",
    firstName: "Mateo",
    lastName: "Silva",
    email: "mateo.silva@example.com",
    company: "Nova Labs",
    status: "engaged",
    score: 88,
    createdAt: "2025-01-10T15:30:00Z"
  },
  {
    id: "33333333-3333-3333-3333-333333333333",
    firstName: "Priya",
    lastName: "Nair",
    email: "priya.nair@example.com",
    company: "Summit Retail",
    status: "qualified",
    score: 95,
    createdAt: "2025-01-08T09:15:00Z"
  }
];

export function LeadTable() {
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
        {placeholderLeads.map((lead) => (
          <TableRow key={lead.id}>
            <TableCell className="font-medium">{lead.firstName} {lead.lastName}</TableCell>
            <TableCell>{lead.company}</TableCell>
            <TableCell className="capitalize">{lead.status}</TableCell>
            <TableCell>{lead.score}</TableCell>
            <TableCell className="text-right">
              <Link className="text-sm font-medium text-primary hover:underline" href={`/leads/${lead.id}`}>
                View
              </Link>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
