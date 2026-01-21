import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export default function SettingsPage() {
  return (
    <div className="space-y-6">
      <div>
        <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Workspace</p>
        <h2 className="text-2xl font-semibold">Settings</h2>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Scoring rules</CardTitle>
          <CardDescription>Adjust weights for lead engagement.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="email-open">Email open score</Label>
            <Input id="email-open" placeholder="5" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="demo-request">Demo request score</Label>
            <Input id="demo-request" placeholder="25" />
          </div>
          <div className="md:col-span-2">
            <Button>Save changes</Button>
          </div>
          {/* TODO: bind to settings API. */}
        </CardContent>
      </Card>
    </div>
  );
}
