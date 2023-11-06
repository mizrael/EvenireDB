targetScope = 'subscription'

param service_name string = 'evenire'
param location string = 'eastus'
param plan_sku string = 'B1'

@allowed([
  'dev'
  'prod'
])
param env string = 'dev'

var resourceGroupName = 'rg-${service_name}'

resource resource_group_resource 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: location
}

module container_registry './container_registry.bicep' = {
  name: 'acr'
  scope: resource_group_resource
  params: {
    location: location
    service_name: service_name
  }
}

module logs './logs.bicep' = {
  name: 'logs'
  scope: resource_group_resource
  params: {
    location: location
    service_name: service_name
  }
}

module webapp './webapp.bicep' = {
  name: 'app'
  dependsOn: [
    container_registry
    logs
  ]
  scope: resource_group_resource
  params: {
    location: location
    service_name: service_name
    plan_sku: plan_sku
    container_registry_name: container_registry.outputs.container_registry_name
    env: env
  }
}
