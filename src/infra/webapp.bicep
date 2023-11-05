param location string
param service_name string
param plan_sku string
param container_registry_name string
param env string

var web_app_name = 'web-${service_name}-${env}'
var web_plan_name = 'plan-${service_name}-${env}'
var always_on = plan_sku != 'F1' 
var image = '${container_registry_name}.azurecr.io/eveniredb:latest'

var appEnv = env == 'dev' ? 'Development' : 'Production'

resource acr 'Microsoft.ContainerRegistry/registries@2023-08-01-preview' existing = {
  name: container_registry_name
}

resource web_plan_resource 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: web_plan_name
  location: location
  sku: {
    name: plan_sku    
    size: plan_sku
  }
  kind: 'linux'
  properties: {
    targetWorkerSizeId: 0
    targetWorkerCount: 1
    reserved: true
    zoneRedundant: false    
  }
}

resource web_app_resource 'Microsoft.Web/sites@2022-09-01' = {
  name: web_app_name
  location: location  
  kind: 'app,linux,container'
  dependsOn:[
    acr
  ]
  properties: {
    serverFarmId: web_plan_resource.id
    enabled: true    
    httpsOnly: true
    siteConfig: {
      numberOfWorkers: 1
      alwaysOn: always_on
      http20Enabled: true      
      http20ProxyFlag: 1  // this is apparently not yet supported, so have to set it manually using CLI after deployment
      linuxFxVersion: 'DOCKER|${image}'
      appSettings:[
        {
          name: 'DOCKER_ENABLE_CI'
          value: 'true'
        }
        {
          name: 'DOCKER_CUSTOM_IMAGE_NAME'
          value: image
        }              
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: acr.properties.loginServer
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_USERNAME'
          value: acr.name
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_PASSWORD'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }        
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: appEnv
        }
      ]
    }
  }
}

var publishingCredentialsId = resourceId('Microsoft.Web/sites/config', web_app_name, 'publishingCredentials')
var publishingcreds = list(publishingCredentialsId, '2022-03-01')
var scmUri = publishingcreds.properties.scmUri
var hook_name = '${service_name}hook'

resource hook 'Microsoft.ContainerRegistry/registries/webhooks@2023-08-01-preview' = {
  parent: acr
  dependsOn:[ web_app_resource ]
  location: location
  name: hook_name
  properties: {
    serviceUri: '${scmUri}/docker/hook'
    status: 'enabled'  
    actions: [
      'push'
    ]
  }
}
