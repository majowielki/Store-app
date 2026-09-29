# E-mails

The notification service (`Services/NotificationService`) turns four events into e-mails: the
order confirmation once it is paid (`OrderPaid`), a refused card with the link back to the payment
(`PaymentDeclined`), the parcel on its way (`OrderShipped`) and "back in stock" for everyone who
asked (`ProductBackInStock`). It has no database and serves nothing but its health endpoints.

## Where the e-mails go

`Mail:Delivery` decides:

| Setting | Where | What happens |
|---------|-------|--------------|
| `Log` | the default: `docker-compose.yml`, Azure | the subject and size are logged, nothing is sent; the shop is a demo and its customers' addresses are made up |
| `Smtp` | `docker-compose.dev.yml` | sent to `Mail:SmtpHost`:`Mail:SmtpPort` - Mailpit, whose inbox is on <http://localhost:8025> |

The sender is `Mail:From`; the links in the e-mails lead to `Shop:Url`. Sending to a real mail
provider means `Mail__Delivery=Smtp` with its host and port; the SMTP sender does not sign in, so
a provider that asks for credentials or TLS needs them added to `MailOptions` and `SmtpMailSender`
first.

## An e-mail that did not arrive

1. The service's log has one line per e-mail (`Sent the OrderPaid e-mail`, or `Would send ...`
   under `Log`), in the trace of the event that caused it, so the order's trace in the Aspire
   dashboard or Application Insights shows whether it was sent.
2. No line: the event did not reach the service. It consumes from its own queues on RabbitMQ; a
   message that failed its retries is in the queue's `_error` queue - see
   [dead-letter-queue.md](dead-letter-queue.md). An SMTP server that refuses the message fails the
   consumer, so the message is retried and then dead-lettered the same way.
3. With no database, the service's inbox is in memory: a message redelivered to the same instance
   is dropped, but one handled just before a restart is handled again, so an e-mail may, rarely,
   come twice. That is the trade for a service with no state.

## Changing an e-mail

The templates are `Mail/Templates/*Mail.cs`, one per event, sharing `MailHtml` (the layout) and
`MailTheme` (the colours). `Tests/Unit/NotificationService` compares each rendered e-mail with its
snapshot in `Snapshots/`; after an intended change run the tests once with `UPDATE_SNAPSHOTS=1`
and commit the new snapshots, which also show the change in the review.
