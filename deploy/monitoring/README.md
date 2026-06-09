# Monitoring — MySQL Slow Queries, Azure Monitor Alerts & Action Groups

## Deploying the Bicep templates

### Enable slow query logging on the MySQL Flexible Server

```bash
az deployment group create \
  --resource-group <rg-name> \
  --template-file mysql-slow-query.bicep \
  --parameters serverName=<mysql-server-name>
```

`long_query_time` defaults to `1` (second). The 500 ms alert threshold is applied in `alerts.bicep` via the KQL query (`query_time_s > 0.5`), keeping the MySQL server log chatty enough to catch borderline queries without flooding the log at sub-second granularity.

### Deploy action groups (once per environment)

```bash
az deployment group create \
  --resource-group <rg-name> \
  --template-file action-groups.bicep \
  --parameters \
      oncallEmail=oncall@yourdomain.com \
      slackWebhookUrl=https://hooks.slack.com/services/... \
      ticketingWebhookUrl=https://your-ticketing-system/webhook
```

The outputs (`p1ActionGroupId`, `p2ActionGroupId`, `p3ActionGroupId`) are passed as `actionGroupId` to `alerts.bicep`.

### Deploy Azure Monitor alert rules

```bash
az deployment group create \
  --resource-group <rg-name> \
  --template-file alerts.bicep \
  --parameters \
      workspaceName=<log-analytics-workspace-name> \
      mysqlServerName=<mysql-server-name> \
      actionGroupId=<action-group-resource-id>
```

## Configuring ConnectionStrings:Read

In Azure App Service / Container Apps, set application settings:

| Key | Value |
|-----|-------|
| `ConnectionStrings__Write` | Primary endpoint connection string |
| `ConnectionStrings__Read` | Read-replica endpoint connection string |

If `ConnectionStrings__Read` is absent the application falls back to the write endpoint — safe for local dev and single-node environments.

## Alert severity mapping

| Bicep severity | Label | Response SLA |
|----------------|-------|--------------|
| 0 | P1 — Critical | Immediate (PagerDuty page) |
| 1 | P1 — Error | Immediate (on-call page) |
| 2 | P2 — Warning | 30 minutes (Slack notification) |
| 3 | P3 — Informational | Next business day (ticket created) |

## Complete alert inventory

| Alert | Severity | Tier | Action |
|-------|----------|------|--------|
| Slow query > 500 ms | 2 | P2 | Slack |
| Connection pool p95 wait > 2 s | 1 | P1 | On-call page |
| API error rate > 5% | 1 | P1 | On-call page |
| API p95 latency > 3 s | 0 | P1 Critical | PagerDuty |
| Hangfire job failures > 5 in 15 min | 2 | P2 | Slack |
| Tenant resolution failures > 10 in 15 min | 3 | P3 | Ticket |
