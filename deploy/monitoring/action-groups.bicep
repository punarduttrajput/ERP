param location string = 'global'  // Action groups are global resources
param oncallEmail string
param slackWebhookUrl string
param ticketingWebhookUrl string

// P1 action group — pages on-call via email + SMS
resource p1ActionGroup 'Microsoft.Insights/actionGroups@2022-06-01' = {
  name: 'erp-oncall-p1'
  location: location
  properties: {
    groupShortName: 'ERP-P1'
    enabled: true
    emailReceivers: [
      {
        name: 'oncall-email'
        emailAddress: oncallEmail
        useCommonAlertSchema: true
      }
    ]
    webhookReceivers: [
      {
        name: 'pagerduty'
        serviceUri: 'https://events.pagerduty.com/integration/{key}/enqueue'
        useCommonAlertSchema: true
      }
    ]
  }
}

// P2 action group — Slack notification
resource p2ActionGroup 'Microsoft.Insights/actionGroups@2022-06-01' = {
  name: 'erp-slack-p2'
  location: location
  properties: {
    groupShortName: 'ERP-P2'
    enabled: true
    webhookReceivers: [
      {
        name: 'slack'
        serviceUri: slackWebhookUrl
        useCommonAlertSchema: true
      }
    ]
  }
}

// P3 action group — create ticket
resource p3ActionGroup 'Microsoft.Insights/actionGroups@2022-06-01' = {
  name: 'erp-ticket-p3'
  location: location
  properties: {
    groupShortName: 'ERP-P3'
    enabled: true
    webhookReceivers: [
      {
        name: 'ticketing'
        serviceUri: ticketingWebhookUrl
        useCommonAlertSchema: true
      }
    ]
  }
}

output p1ActionGroupId string = p1ActionGroup.id
output p2ActionGroupId string = p2ActionGroup.id
output p3ActionGroupId string = p3ActionGroup.id
