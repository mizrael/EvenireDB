param location string
param service_name string

var container_registry_name = 'cr${service_name}'

resource container_registry_resource 'Microsoft.ContainerRegistry/registries@2023-08-01-preview' = {
  name: container_registry_name
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true   
  }
}

output container_registry_name string = container_registry_name
