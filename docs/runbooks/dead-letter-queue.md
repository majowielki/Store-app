# A message in an error queue

Every consumer gets three attempts with exponential back-off; a message that fails all of them
lands in the `_error` queue next to its own (`cart-order-placed_error`,
`identity-order-placed_error`, `audit-order-placed_error`, `audit-audit-event_error`). Alert 4
in [../observability.md](../observability.md) fires as soon as one of them is not empty.

1. Read the fault: the message in the error queue carries the exception in its headers
   (`MT-Fault-Message`, `MT-Fault-StackTrace`); the management UI shows it under *Get messages*,
   as does `rabbitmqadmin get queue=cart-order-placed_error count=1`. The trace id in the headers
   finds the consumer's spans in Application Insights.
2. Fix the cause: a bug in the consumer (deploy), a product that no longer exists (data), a
   dependency that was down (nothing to fix).
3. Move the messages back: the RabbitMQ *Shovel* plugin from `<queue>_error` to `<queue>`, or,
   for a handful of messages, requeue them from the management UI. Redelivery is safe: a consumer
   that already handled a message finds it in its inbox and ignores it.
4. When the message must not be processed (a test event, a request that was withdrawn), purge
   the error queue and leave a note in the audit trail, which is where such decisions are kept.

Never delete an error queue itself: MassTransit recreates it, but the messages in it are gone.
