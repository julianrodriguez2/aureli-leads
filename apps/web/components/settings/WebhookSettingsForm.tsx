"use client";

import { useState } from "react";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { updateWebhookSettings, testWebhook } from "@/lib/auth";
import type { SettingsDto, UpdateSettingsRequest } from "@/lib/types";
import { toastApiError, toastError, toastSuccess } from "@/lib/toast";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const schema = z.object({
  webhookTargetUrl: z
    .string()
    .trim()
    .min(1, "Webhook URL is required")
    .max(500, "Webhook URL is too long")
    .refine((value) => value.startsWith("http://") || value.startsWith("https://"), {
      message: "Webhook URL must start with http:// or https://"
    }),
  webhookSecret: z
    .preprocess((value) => {
      if (typeof value === "string") {
        const trimmed = value.trim();
        return trimmed.length === 0 ? undefined : trimmed;
      }
      return value;
    }, z.string().max(200, "Secret too long").optional())
    .refine((value) => !value || value.length >= 8, {
      message: "Secret must be at least 8 characters"
    })
});

type FormValues = z.infer<typeof schema>;

type WebhookSettingsFormProps = {
  initialSettings: SettingsDto | null;
  isAdmin: boolean;
  errorMessage?: string | null;
  errorTraceId?: string | null;
};

export function WebhookSettingsForm({ initialSettings, isAdmin, errorMessage, errorTraceId }: WebhookSettingsFormProps) {
  const [hasWebhookSecret, setHasWebhookSecret] = useState(initialSettings?.hasWebhookSecret ?? false);
  const [isSaving, setIsSaving] = useState(false);
  const [isTesting, setIsTesting] = useState(false);
  const [isRotating, setIsRotating] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      webhookTargetUrl: initialSettings?.webhookTargetUrl ?? "",
      webhookSecret: ""
    }
  });

  async function handleSave(values: FormValues) {
    if (!isAdmin) {
      toastError("Admin access required.");
      return;
    }

    setIsSaving(true);
    try {
      const payload: UpdateSettingsRequest = {
        webhookTargetUrl: values.webhookTargetUrl.trim()
      };

      if (values.webhookSecret) {
        payload.webhookSecret = values.webhookSecret;
      }

      const response = await updateWebhookSettings(payload);
      setHasWebhookSecret(response.hasWebhookSecret ?? false);
      form.reset({
        webhookTargetUrl: response.webhookTargetUrl ?? payload.webhookTargetUrl,
        webhookSecret: ""
      });
      toastSuccess("Webhook settings saved.");
    } catch (error) {
      toastApiError(error, "Unable to save webhook settings.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleRotate() {
    if (!isAdmin) {
      toastError("Admin access required.");
      return;
    }

    const values = form.getValues();
    const parsed = schema.safeParse(values);
    if (!parsed.success) {
      toastError(parsed.error.errors[0]?.message ?? "Fix validation errors first.");
      return;
    }

    setIsRotating(true);
    try {
      const response = await updateWebhookSettings({
        webhookTargetUrl: values.webhookTargetUrl.trim(),
        rotateSecret: true
      });
      setHasWebhookSecret(response.hasWebhookSecret ?? true);
      form.reset({
        webhookTargetUrl: response.webhookTargetUrl ?? values.webhookTargetUrl,
        webhookSecret: ""
      });
      toastSuccess("Webhook secret rotated.");
    } catch (error) {
      toastApiError(error, "Unable to rotate secret.");
    } finally {
      setIsRotating(false);
    }
  }

  async function handleTest() {
    if (!isAdmin) {
      toastError("Admin access required.");
      return;
    }

    setIsTesting(true);
    try {
      const response = await testWebhook();
      if (response.ok) {
        toastSuccess(`Test webhook delivered (${response.statusCode}).`);
      } else {
        toastError(response.error ?? `Test webhook failed (${response.statusCode}).`);
      }
    } catch (error) {
      toastApiError(error, "Unable to send test webhook.");
    } finally {
      setIsTesting(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Webhook configuration</CardTitle>
        <CardDescription>Configure the URL and secret used for outbound webhooks.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        {errorMessage ? (
          <div className="rounded-xl border border-dashed border-border/70 bg-white/80 p-4 text-sm text-muted-foreground">
            <p>{errorMessage}</p>
            {errorTraceId ? (
              <p className="mt-2 text-xs text-muted-foreground">Trace ID: {errorTraceId}</p>
            ) : null}
          </div>
        ) : (
          <form onSubmit={form.handleSubmit(handleSave)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="webhookTargetUrl">Webhook Target URL</Label>
              <Input
                id="webhookTargetUrl"
                placeholder="https://your-n8n/webhook/..."
                disabled={!isAdmin || isSaving || isRotating}
                {...form.register("webhookTargetUrl")}
              />
              {form.formState.errors.webhookTargetUrl ? (
                <p className="text-xs text-rose-600">{form.formState.errors.webhookTargetUrl.message}</p>
              ) : null}
            </div>
            <div className="space-y-2">
              <Label htmlFor="webhookSecret">Webhook Secret</Label>
              <Input
                id="webhookSecret"
                type="password"
                placeholder={hasWebhookSecret ? "Secret is set (enter to replace)" : "Enter a secret"}
                disabled={!isAdmin || isSaving || isRotating}
                {...form.register("webhookSecret")}
              />
              {form.formState.errors.webhookSecret ? (
                <p className="text-xs text-rose-600">{form.formState.errors.webhookSecret.message}</p>
              ) : null}
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <Button type="submit" disabled={!isAdmin || isSaving || isRotating}>
                {isSaving ? "Saving..." : "Save"}
              </Button>
              <Button type="button" variant="secondary" onClick={handleRotate} disabled={!isAdmin || isRotating || isSaving}>
                {isRotating ? "Rotating..." : "Rotate Secret"}
              </Button>
              <Button type="button" variant="outline" onClick={handleTest} disabled={!isAdmin || isTesting || isSaving}>
                {isTesting ? "Sending..." : "Send Test Webhook"}
              </Button>
              {!isAdmin ? (
                <span className="text-xs text-muted-foreground">Admin access required to update settings.</span>
              ) : null}
            </div>
          </form>
        )}
      </CardContent>
    </Card>
  );
}
