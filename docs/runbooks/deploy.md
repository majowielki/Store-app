# Deploying to Azure Container Apps

Everything the store needs on Azure is described in `infra/bicep/main.bicep` (see the comment at
its top for the list of resources) and applied by `.github/workflows/cd.yml`. Nothing is done in
the portal after the one-time preparation below.

## One-time preparation

1. **PostgreSQL** - an Azure Database for PostgreSQL flexible server with the eight databases
   (`store_identity_db`, `store_product_db`, `store_cart_db`, `store_order_db`, `store_audit_db`,
   `store_content_db`, `store_payment_db`, `store_review_db`)
   owned by the application user; `Infrastructure/scripts/init-databases.sql` creates them.
   Automatic backups: 7 days at least, geo-redundant for production (see
   [database-restore.md](database-restore.md)). Allow the Container Apps environment's outbound
   addresses or use a private endpoint; `Ssl Mode=Require` is in the connection strings.
   The catalogue's search migration creates the `unaccent` and `fuzzystrmatch` extensions: allow
   them in the server parameter `azure.extensions` first
   (`az postgres flexible-server parameter set -g <rg> -s <server> -n azure.extensions -v UNACCENT,FUZZYSTRMATCH`).
2. **A service principal for GitHub** with a federated credential (OpenID Connect) for the
   repository's `production` environment:
   ```bash
   az ad app create --display-name store-github-cd
   az ad sp create --id <appId>
   az ad app federated-credential create --id <appId> --parameters '{
     "name": "github-production", "issuer": "https://token.actions.githubusercontent.com",
     "subject": "repo:<owner>/<repo>:environment:production", "audiences": ["api://AzureADTokenExchange"] }'
   ```
   Roles on the resource group: `Contributor`, `User Access Administrator` (for the two role
   assignments in the template) and `Key Vault Secrets Officer` (the template writes the
   secrets); `AcrPush` on the registry once it exists - the first deployment creates it, so
   either create the registry by hand first or grant the role after the first run and run the
   workflow again.
3. **Repository settings** - environment `production` with a required reviewer; the variables
   and secrets listed at the top of `cd.yml`. Generate `JWT_SECRET_KEY`, `INTERNAL_API_KEY` and
   `PAYMENT_WEBHOOK_SECRET` with `openssl rand -base64 48`; the template hands the same value to
   every app that needs it.

## What a deployment does

1. `az acr build` builds the eleven images in the registry, tagged with the commit SHA.
2. For every service, its migration job (`<service>-migrate`: the service image started with
   `--migrate`) is updated to the new image and run; the pipeline waits for `Succeeded`. A failed
   migration stops the deployment before any service changes.
3. `az deployment group create` applies the template: new revisions of the apps with the new
   images. A service in `Production` refuses to start against a schema it does not know, so a
   revision that came up before its migration stays unhealthy and the previous one keeps serving.
4. `Scripts/Upload-Blobs.ps1 -AuthMode key` uploads `Blobs/` (the pictures and their smaller
   copies) to the public `product-images` container of the pictures account (the template's output
   `picturesAccount`), replacing what is there. The catalogue's and the content service's demo data
   point there: the template gives them `Pictures__BaseUrl`, the container's address.
5. The smoke test asks the gateway's `/health/ready`, the catalogue through the UI and the first
   product's picture.

The first deployment has no jobs yet: the template creates them, the workflow runs them once and
restarts the services' revisions.

The pictures account is not the environment's storage account: that one keeps the RabbitMQ share
behind its keys and allows no anonymous read, while anyone may read a picture by its address (not
list the container). The seed writes whole addresses into the databases, so a later change of the
container's address reaches only what is seeded after it; existing rows need an update by hand.

## After a deployment

- `az containerapp logs show -g Store-App -n orderservice --follow` streams a service's log;
  Application Insights (`store-insights`) has the traces, and [../observability.md](../observability.md)
  the queries.
- `az containerapp revision list -g Store-App -n gateway -o table` shows which revision serves.

## Rollback

Every image keeps its SHA tag, so a rollback is a deployment of an earlier tag: run the CD
workflow (`workflow_dispatch`) on the earlier commit, or by hand

```bash
IMAGE_TAG=<previous sha> az deployment group create -g Store-App -f infra/bicep/main.bicep -p infra/bicep/main.bicepparam
```

with the same secrets in the environment as the workflow uses (`main.bicepparam` reads them).
Migrations are forward-only (expand, then contract in a later release): a schema change is
written so that the previous version of the service still runs against it, which is what makes
rolling back the image enough.
