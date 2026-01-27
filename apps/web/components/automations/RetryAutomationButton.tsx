"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { ApiError } from "@/lib/api";
import { retryAutomationEvent } from "@/lib/auth";
import { Button } from "@/components/ui/button";

type RetryAutomationButtonProps = {
  eventId: string;
  eventType: string;
  leadId: string;
  attemptCount: number;
  lastError?: string | null;
};

function truncate(value?: string | null, length = 120) {
  if (!value) {
    return "None";
  }

  return value.length > length ? `${value.slice(0, length)}...` : value;
}

export function RetryAutomationButton({
  eventId,
  eventType,
  leadId,
  attemptCount,
  lastError
}: RetryAutomationButtonProps) {
  const router = useRouter();
  const [isOpen, setIsOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleConfirm() {
    setIsSubmitting(true);
    try {
      await retryAutomationEvent(eventId);
      toast.success("Queued for retry.");
      setIsOpen(false);
      router.refresh();
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 403) {
          toast.error("You do not have permission to retry.");
        } else if (error.status === 404) {
          toast.error("Automation event not found.");
        } else if (error.status === 409) {
          toast.error("Already sent.");
        } else {
          toast.error("Unable to retry this event.");
        }
      } else {
        toast.error("Unable to retry this event.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <>
      <Button variant="outline" size="sm" onClick={() => setIsOpen(true)}>
        Retry
      </Button>
      {isOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          role="dialog"
          aria-modal="true"
          onClick={() => {
            if (!isSubmitting) {
              setIsOpen(false);
            }
          }}
        >
          <div
            className="w-full max-w-md rounded-2xl border border-border bg-background p-6 shadow-xl"
            onClick={(event) => event.stopPropagation()}
          >
            <h3 className="text-lg font-semibold">Retry webhook?</h3>
            <div className="mt-3 space-y-2 text-sm text-muted-foreground">
              <p><span className="font-medium text-foreground">Event:</span> {eventType}</p>
              <p><span className="font-medium text-foreground">Lead:</span> {leadId}</p>
              <p><span className="font-medium text-foreground">Attempts:</span> {attemptCount}</p>
              <p><span className="font-medium text-foreground">Last error:</span> {truncate(lastError)}</p>
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <Button variant="ghost" onClick={() => setIsOpen(false)} disabled={isSubmitting}>
                Cancel
              </Button>
              <Button onClick={handleConfirm} disabled={isSubmitting}>
                {isSubmitting ? "Queueing..." : "Confirm"}
              </Button>
            </div>
          </div>
        </div>
      ) : null}
    </>
  );
}
