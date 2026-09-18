// The store on Azure Container Apps, in one resource group. What it creates:
//
//   - a Log Analytics workspace and Application Insights; the Container Apps environment sends
//     its logs there and its managed OpenTelemetry agent forwards the traces and logs the hosts
//     export through OTLP (see docs/observability.md),
//   - a container registry the CD pipeline pushes to, and one user-assigned identity every app
//     uses to pull from it and to read its secrets,
//   - a Key Vault holding every secret the apps need; the apps reference the secrets by URL,
//     nothing secret is written into an app's definition,
//   - the Container Apps environment,
//   - RabbitMQ as a single-replica app with a durable Azure Files volume,
//   - the five services with internal ingress (unreachable from the internet), the gateway
//     and the UI with external ingress,
//   - one job per service that runs its migrations (`--migrate`) - the pipeline starts them
//     before it updates the services.
//
// PostgreSQL is not created here: the flexible server holds the data and outlives every
// deployment; its host, user and password are parameters.
//
//   az deployment group create -g <rg> -f infra/bicep/main.bicep -p infra/bicep/main.bicepparam

targetScope = 'resourceGroup'

@description('Region of everything; the resource group\'s by default')
param location string = resourceGroup().location

@description('Prefix of the resource names; short, because a storage account name is limited to 24 characters')
@maxLength(9)
param baseName string = 'store'

@description('Tag of the images to run, as pushed by the pipeline (the commit SHA)')
param imageTag string

@description('Host name of the PostgreSQL flexible server, e.g. storeapp-db.postgres.database.azure.com')
param postgresHost string

param postgresUser string = 'store_user'

@secure()
param postgresPassword string

@secure()
@minLength(32)
param jwtSecretKey string

@secure()
@minLength(32)
param internalApiKey string

@secure()
param trueAdminPassword string

param trueAdminEmail string = 'trueadmin@store.com'

@secure()
param rabbitMqPassword string

param rabbitMqUser string = 'store_user'

@description('The showcase accounts and the password-less demo logins')
param demoEnabled bool = false

@description('Origins allowed to call the API from a browser; empty for the normal setup, where the UI proxies /api itself')
param corsAllowedOrigins array = []

var acrName = replace('${baseName}acr${uniqueString(resourceGroup().id)}', '-', '')
var keyVaultName = take('${baseName}-kv-${uniqueString(resourceGroup().id)}', 24)
var storageAccountName = replace('${baseName}st${uniqueString(resourceGroup().id)}', '-', '')

// --- monitoring -----------------------------------------------------------------------------

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${baseName}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// --- identity, registry, secrets --------------------------------------------------------------

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${baseName}-apps'
  location: location
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

// AcrPull
resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, identity.id, 'AcrPull')
  scope: acr
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: true
  }
}

// Key Vault Secrets User
resource secretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, identity.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

var databases = ['identity', 'product', 'cart', 'order', 'audit']

resource connectionSecrets 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [
  for db in databases: {
    parent: keyVault
    name: 'postgres-connection-${db}'
    properties: {
      value: 'Host=${postgresHost};Database=store_${db}_db;Username=${postgresUser};Password=${postgresPassword};Port=5432;Ssl Mode=Require'
    }
  }
]

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-secret-key'
  properties: {
    value: jwtSecretKey
  }
}

resource internalApiSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'internal-api-key'
  properties: {
    value: internalApiKey
  }
}

resource trueAdminSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'true-admin-password'
  properties: {
    value: trueAdminPassword
  }
}

resource rabbitMqSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'rabbitmq-password'
  properties: {
    value: rabbitMqPassword
  }
}

// --- the environment ------------------------------------------------------------------------

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource rabbitMqShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: 'rabbitmq'
  properties: {
    shareQuota: 5
  }
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' = {
  name: '${baseName}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    // The managed OpenTelemetry agent: the hosts export OTLP to the endpoint the environment
    // injects (OTEL_EXPORTER_OTLP_ENDPOINT); the agent forwards to Application Insights
    appInsightsConfiguration: {
      connectionString: appInsights.properties.ConnectionString
    }
    openTelemetryConfiguration: {
      tracesConfiguration: {
        destinations: ['appInsights']
      }
      logsConfiguration: {
        destinations: ['appInsights']
      }
    }
    zoneRedundant: false
  }
}

resource rabbitMqStorage 'Microsoft.App/managedEnvironments/storages@2025-01-01' = {
  parent: managedEnvironment
  name: 'rabbitmq'
  properties: {
    azureFile: {
      accountName: storage.name
      accountKey: storage.listKeys().keys[0].value
      shareName: rabbitMqShare.name
      accessMode: 'ReadWrite'
    }
  }
}

// --- RabbitMQ -------------------------------------------------------------------------------

// One replica with a durable volume: the outbox in every service buffers events while the
// broker restarts, so a single instance is enough and a second one would split the queues
resource rabbitMq 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'rabbitmq'
  location: location
  properties: {
    managedEnvironmentId: managedEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5672
        exposedPort: 5672
        transport: 'tcp'
      }
      secrets: [
        {
          name: 'rabbitmq-password'
          keyVaultUrl: '${vaultUri}secrets/rabbitmq-password'
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'rabbitmq'
          image: 'rabbitmq:3-management-alpine'
          env: [
            { name: 'RABBITMQ_DEFAULT_USER', value: rabbitMqUser }
            { name: 'RABBITMQ_DEFAULT_PASS', secretRef: 'rabbitmq-password' }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          volumeMounts: [
            {
              volumeName: 'data'
              mountPath: '/var/lib/rabbitmq'
            }
          ]
        }
      ]
      volumes: [
        {
          name: 'data'
          storageType: 'AzureFile'
          storageName: rabbitMqStorage.name
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  dependsOn: [secretsUser]
}

// --- the services ---------------------------------------------------------------------------

// Known before anything is deployed, so the loops below can iterate over the services
var vaultUri = 'https://${keyVaultName}${az.environment().suffixes.keyvaultDns}/'
var internal = 'internal.${managedEnvironment.properties.defaultDomain}'

var commonEnv = [
  { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
  { name: 'JwtSettings__SecretKey', secretRef: 'jwt-secret-key' }
  { name: 'JwtSettings__Issuer', value: 'Store.API' }
  { name: 'JwtSettings__Audience', value: 'Store.Client' }
]

var messagingEnv = [
  { name: 'RabbitMQ__Host', value: 'rabbitmq' }
  { name: 'RabbitMQ__Port', value: '5672' }
  { name: 'RabbitMQ__VirtualHost', value: '/' }
  { name: 'RabbitMQ__Username', value: rabbitMqUser }
  { name: 'RabbitMQ__Password', secretRef: 'rabbitmq-password' }
]

var commonSecrets = [
  { name: 'jwt-secret-key', keyVaultUrl: '${vaultUri}secrets/jwt-secret-key' }
  { name: 'rabbitmq-password', keyVaultUrl: '${vaultUri}secrets/rabbitmq-password' }
]

var internalApiSecretRef = { name: 'internal-api-key', keyVaultUrl: '${vaultUri}secrets/internal-api-key' }

// Every service: which database, which extra variables and secrets, which image. The
// addresses of the neighbours are added in the loop, since the environment's domain is only
// known once it exists.
var services = [
  {
    name: 'identityservice'
    image: 'store/identity'
    db: 'identity'
    callsCatalog: false
    callsCart: false
    env: [
      { name: 'TrueAdmin__Email', value: trueAdminEmail }
      { name: 'TrueAdmin__Password', secretRef: 'true-admin-password' }
      { name: 'Demo__Enabled', value: string(demoEnabled) }
    ]
    secrets: [
      { name: 'true-admin-password', keyVaultUrl: '${vaultUri}secrets/true-admin-password' }
    ]
  }
  {
    name: 'productservice'
    image: 'store/product'
    db: 'product'
    callsCatalog: false
    callsCart: false
    env: [
      { name: 'InternalApi__ApiKey', secretRef: 'internal-api-key' }
    ]
    secrets: [internalApiSecretRef]
  }
  {
    name: 'cartservice'
    image: 'store/cart'
    db: 'cart'
    callsCatalog: true
    callsCart: false
    env: [
      { name: 'InternalApi__ApiKey', secretRef: 'internal-api-key' }
    ]
    secrets: [internalApiSecretRef]
  }
  {
    name: 'orderservice'
    image: 'store/order'
    db: 'order'
    callsCatalog: true
    callsCart: true
    env: [
      { name: 'InternalApi__ApiKey', secretRef: 'internal-api-key' }
    ]
    secrets: [internalApiSecretRef]
  }
  {
    name: 'auditlogservice'
    image: 'store/audit'
    db: 'audit'
    callsCatalog: false
    callsCart: false
    env: []
    secrets: []
  }
]

var catalogAddress = [{ name: 'Services__ProductService', value: 'http://productservice.${internal}' }]
var cartAddress = [{ name: 'Services__CartService', value: 'http://cartservice.${internal}' }]
var corsEnv = [for (origin, i) in corsAllowedOrigins: { name: 'Cors__AllowedOrigins__${i}', value: origin }]

module serviceApps 'app.bicep' = [
  for service in services: {
    name: 'app-${service.name}'
    params: {
      name: service.name
      location: location
      environmentId: managedEnvironment.id
      identityId: identity.id
      acrLoginServer: acr.properties.loginServer
      image: '${acr.properties.loginServer}/${service.image}:${imageTag}'
      external: false
      env: concat(
        commonEnv,
        messagingEnv,
        [{ name: 'ConnectionStrings__DefaultConnection', secretRef: 'db-connection' }],
        service.env,
        service.callsCatalog ? catalogAddress : [],
        service.callsCart ? cartAddress : []
      )
      keyVaultSecrets: concat(
        commonSecrets,
        [{ name: 'db-connection', keyVaultUrl: '${vaultUri}secrets/postgres-connection-${service.db}' }],
        service.secrets
      )
    }
    dependsOn: [acrPull, secretsUser, connectionSecrets, rabbitMq]
  }
]

module migrationJobs 'job.bicep' = [
  for service in services: {
    name: 'job-${service.name}-migrate'
    params: {
      name: '${service.name}-migrate'
      location: location
      environmentId: managedEnvironment.id
      identityId: identity.id
      acrLoginServer: acr.properties.loginServer
      image: '${acr.properties.loginServer}/${service.image}:${imageTag}'
      env: concat(
        commonEnv,
        messagingEnv,
        [{ name: 'ConnectionStrings__DefaultConnection', secretRef: 'db-connection' }],
        service.env,
        service.callsCatalog ? catalogAddress : [],
        service.callsCart ? cartAddress : []
      )
      keyVaultSecrets: concat(
        commonSecrets,
        [{ name: 'db-connection', keyVaultUrl: '${vaultUri}secrets/postgres-connection-${service.db}' }],
        service.secrets
      )
    }
    dependsOn: [acrPull, secretsUser, connectionSecrets]
  }
]

// --- gateway and UI -------------------------------------------------------------------------

module gateway 'app.bicep' = {
  name: 'app-gateway'
  params: {
    name: 'gateway'
    location: location
    environmentId: managedEnvironment.id
    identityId: identity.id
    acrLoginServer: acr.properties.loginServer
    image: '${acr.properties.loginServer}/store/gateway:${imageTag}'
    external: true
    maxReplicas: 5
    env: concat(commonEnv, [
      { name: 'ReverseProxy__Clusters__identity-cluster__Destinations__destination1__Address', value: 'http://identityservice.${internal}/' }
      { name: 'ReverseProxy__Clusters__products-cluster__Destinations__destination1__Address', value: 'http://productservice.${internal}/' }
      { name: 'ReverseProxy__Clusters__cart-cluster__Destinations__destination1__Address', value: 'http://cartservice.${internal}/' }
      { name: 'ReverseProxy__Clusters__orders-cluster__Destinations__destination1__Address', value: 'http://orderservice.${internal}/' }
      { name: 'ReverseProxy__Clusters__audit-cluster__Destinations__destination1__Address', value: 'http://auditlogservice.${internal}/' }
    ], corsEnv)
    keyVaultSecrets: [
      { name: 'jwt-secret-key', keyVaultUrl: '${vaultUri}secrets/jwt-secret-key' }
    ]
  }
  dependsOn: [acrPull, secretsUser]
}

module ui 'app.bicep' = {
  name: 'app-ui'
  params: {
    name: 'ui'
    location: location
    environmentId: managedEnvironment.id
    identityId: identity.id
    acrLoginServer: acr.properties.loginServer
    image: '${acr.properties.loginServer}/store/ui:${imageTag}'
    external: true
    cpu: '0.25'
    memory: '0.5Gi'
    env: [
      // nginx proxies /api to the gateway inside the environment
      { name: 'GATEWAY_UPSTREAM', value: 'https://gateway.${managedEnvironment.properties.defaultDomain}' }
      { name: 'API_BASE_URL', value: '/api/v1' }
    ]
    // nginx answers on /, not on /health
    probePath: '/'
    livenessPath: '/'
  }
  dependsOn: [acrPull]
}

output uiUrl string = 'https://${ui.outputs.fqdn}'
output gatewayUrl string = 'https://${gateway.outputs.fqdn}'
output containerRegistry string = acr.properties.loginServer
output keyVault string = keyVault.name
output environmentName string = managedEnvironment.name
output migrationJobs array = [for (service, i) in services: migrationJobs[i].outputs.name]
