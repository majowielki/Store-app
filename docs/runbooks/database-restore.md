# Backups and restoring a database

## Policy

| Store | Backup | Where |
|-------|--------|-------|
| PostgreSQL flexible server | automatic, daily plus continuous WAL; retention 7 days (35 for production), geo-redundant for production | the server's *Backup and restore* blade |
| RabbitMQ | none: queues are durable and messages persistent on the Azure Files volume, and whatever a broker loses is still in the services' outboxes and is sent again | - |
| Audit log | `AuditRetentionService` keeps 90 days; export older rows to blob storage before they go when they must be kept longer | `store_audit_db` |
| Secrets | Key Vault with soft delete and purge protection: a deleted secret can be recovered for 90 days | `store-kv-*` |
| Migrations | code in the repository, never regenerated (`Services/*/Migrations`) | git |

Each service has its own database, so one can be restored without touching the others.

## Restoring one service's database to a point in time

1. Stop the service: `az containerapp update -g Store-App -n orderservice --min-replicas 0 --max-replicas 0`.
   The outboxes of the other services keep the events meant for it meanwhile.
2. Restore the server to a new server at the point in time:
   `az postgres flexible-server restore -g Store-App -n storeapp-db-restore --source-server storeapp-db --restore-time "2026-09-18T06:00:00Z"`.
   A flexible server restores as a whole; only the one database is taken from the copy.
3. Copy the database over: `pg_dump -h <restored host> -U store_user -Fc store_order_db > order.dump`,
   then on the live server `dropdb store_order_db && createdb -O store_user store_order_db` and
   `pg_restore -h <live host> -U store_user -d store_order_db order.dump`. The outbox and inbox
   tables come with the dump: an event that was in the outbox at the restore point is delivered
   again after the restart, and the consumers' inboxes drop what they have already handled.
4. Start the service again (`--min-replicas 1 --max-replicas 3`), watch `/health/ready` and the
   traces, then delete the restored server.

## Testing the restore

Quarterly, and after every change to the migrations: restore the previous day's backup of the
production server to a throw-away server, run every service's migration job against it (a copy
of the job with `ConnectionStrings__DefaultConnection` pointing at the copy) and start one service
against it to see it pass `/health/ready`. Record the date and how long it took:

| Date | Restored to | Time to ready | Notes |
|------|-------------|---------------|-------|
| - | - | - | not performed yet |
