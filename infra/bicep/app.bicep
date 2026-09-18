// One Container App of the store: a service, the gateway or the UI. Internal by default (the
// FQDN resolves only inside the environment); pulls its image with the shared managed identity,
// reads its secrets from Key Vault with the same identity, and is probed on /health.

param name string
param location string
param environmentId string
param identityId string
param acrLoginServer string
param image string
@description('Reachable from the internet (gateway, UI) or only from the environment (services)')
param external bool = false
param minReplicas int = 1
param maxReplicas int = 3
param cpu string = '0.5'
param memory string = '1Gi'
@description('Environment variables: { name, value } or { name, secretRef }')
param env array = []
@description('Secrets taken from Key Vault: { name, keyVaultUrl }')
param keyVaultSecrets array = []
@description('Where the readiness and liveness probes look; the UI has no /health')
param probePath string = '/health/ready'
param livenessPath string = '/health/live'

resource app 'Microsoft.App/containerApps@2025-01-01' = {
  name: name
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: external
        targetPort: 8080
        transport: 'auto'
        // Traffic between the gateway and the services stays inside the environment; plain
        // HTTP there saves a TLS hop and a redirect the proxy would not follow
        allowInsecure: !external
      }
      registries: [
        {
          server: acrLoginServer
          identity: identityId
        }
      ]
      secrets: [
        for secret in keyVaultSecrets: {
          name: secret.name
          keyVaultUrl: secret.keyVaultUrl
          identity: identityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: name
          image: image
          env: env
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: livenessPath
                port: 8080
              }
              periodSeconds: 5
              failureThreshold: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: probePath
                port: 8080
              }
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'Liveness'
              httpGet: {
                path: livenessPath
                port: 8080
              }
              periodSeconds: 30
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

output fqdn string = app.properties.configuration.ingress.fqdn
output name string = app.name
