param workspaceName string
param mysqlServerName string
param location string = resourceGroup().location
param actionGroupId string  // PagerDuty / email action group resource ID

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: workspaceName
}

resource slowQueryAlert 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = {
  name: 'erp-mysql-slow-query-alert'
  location: location
  properties: {
    displayName: 'ERP MySQL Slow Query Alert (> 500ms)'
    severity: 2  // Warning
    enabled: true
    scopes: [workspace.id]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'AzureDiagnostics | where Category == "MySqlSlowLogs" | where query_time_s > 0.5 | summarize count() by bin(TimeGenerated, 5m)'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [actionGroupId]
    }
  }
}

resource connectionPoolAlert 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = {
  name: 'erp-db-connection-pool-alert'
  location: location
  properties: {
    displayName: 'ERP DB Connection Pool Near Exhaustion'
    severity: 1  // Error
    enabled: true
    scopes: [workspace.id]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'customMetrics | where name == "db_connection_wait_ms" | summarize percentile(value, 95) by bin(TimeGenerated, 1m) | where percentile_value_95 > 2000'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [actionGroupId]
    }
  }
}

resource errorRateAlert 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = {
  name: 'erp-api-error-rate-alert'
  location: location
  properties: {
    displayName: 'ERP API Error Rate > 5%'
    severity: 1
    enabled: true
    scopes: [workspace.id]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'requests | summarize errorRate = countif(success == false) * 100.0 / count() by bin(timestamp, 1m) | where errorRate > 5'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 2
          }
        }
      ]
    }
    actions: {
      actionGroups: [actionGroupId]
    }
  }
}

// P1: API p95 latency > 3 seconds (page on-call immediately)
resource p95LatencyAlert 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = {
  name: 'erp-api-p95-latency-p1'
  location: location
  properties: {
    displayName: 'ERP API p95 Latency > 3s [P1]'
    severity: 0  // Critical — pages on-call
    enabled: true
    scopes: [workspace.id]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'requests | summarize p95 = percentile(duration, 95) by bin(timestamp, 1m) | where p95 > 3000'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 2
          }
        }
      ]
    }
    actions: {
      actionGroups: [actionGroupId]
    }
  }
}

// P2: Failed background jobs (Hangfire) — Slack notification
resource hangfireJobFailureAlert 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = {
  name: 'erp-hangfire-job-failures-p2'
  location: location
  properties: {
    displayName: 'ERP Hangfire Job Failures > 5 [P2]'
    severity: 2
    enabled: true
    scopes: [workspace.id]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      allOf: [
        {
          query: 'traces | where message contains "BackgroundServerProcess" and message contains "Failed state" | summarize count() by bin(timestamp, 5m) | where count_ > 5'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [actionGroupId]
    }
  }
}

// P3: Tenant resolution failures (misconfigured subdomains) — create ticket
resource tenantResolutionAlert 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = {
  name: 'erp-tenant-resolution-failures-p3'
  location: location
  properties: {
    displayName: 'ERP Tenant Resolution Failures > 10 [P3]'
    severity: 3
    enabled: true
    scopes: [workspace.id]
    evaluationFrequency: 'PT15M'
    windowSize: 'PT15M'
    criteria: {
      allOf: [
        {
          query: 'traces | where message contains "Tenant not found for slug" | summarize count() by bin(timestamp, 15m) | where count_ > 10'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [actionGroupId]
    }
  }
}
