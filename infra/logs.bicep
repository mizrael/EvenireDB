param location string
param service_name string

var logs_name = 'logs-${service_name}'

resource logs_resource 'microsoft.insights/components@2020-02-02' = {
  name: logs_name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'IbizaAIExtension'
    RetentionInDays: 30
    IngestionMode: 'ApplicationInsights'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output logs_name string = logs_name
