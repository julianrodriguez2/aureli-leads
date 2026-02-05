# Architecture Notes

## Core Entities
- **User**: authentication, roles (Admin/Agent/ReadOnly)
- **Lead**: primary CRM record
- **LeadActivity**: audit trail (notes, scoring, status changes, webhook results)
- **AutomationEvent**: outbound webhook queue with retry metadata
- **Setting**: configuration key/value (webhook target + secret)
- **SettingsActivity**: audit log for admin actions

## Automation Reliability Pattern
1. API creates an **AutomationEvent** with `Status=Pending` and a payload snapshot.
2. Background dispatcher polls pending events and attempts delivery.
3. Success transitions to `Sent`; failure updates `LastError` and retries until max attempts.

## Dispatcher Retry Logic
- Max attempts: 10
- Minimum delay between attempts: 15 seconds
- Simple exponential backoff capped at 60 seconds

## Notes
This is intentionally lightweight for demo purposes. A production build would replace the polling dispatcher with a durable queue (e.g., SQS, RabbitMQ) and structured observability.
