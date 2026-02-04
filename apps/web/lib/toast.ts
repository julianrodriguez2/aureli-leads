import { toast } from "sonner";
import { ApiError } from "@/lib/api";

function buildTraceDescription(error?: unknown) {
  if (error instanceof ApiError && error.traceId) {
    return `Trace ID: ${error.traceId}`;
  }

  return undefined;
}

export function toastSuccess(message: string) {
  toast.success(message);
}

export function toastError(message: string, error?: unknown) {
  const description = buildTraceDescription(error);
  if (description) {
    toast.error(message, { description });
    return;
  }

  toast.error(message);
}

export function toastApiError(error: unknown, fallbackMessage: string) {
  if (error instanceof ApiError) {
    const message = error.message || fallbackMessage;
    const description = buildTraceDescription(error);
    if (description) {
      toast.error(message, { description });
      return;
    }
    toast.error(message);
    return;
  }

  toast.error(fallbackMessage);
}
