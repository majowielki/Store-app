// Parameters of main.bicep. The secrets are read from the environment, so this file holds none:
//
//   IMAGE_TAG=<sha> POSTGRES_HOST=... POSTGRES_PASSWORD=... JWT_SECRET_KEY=... INTERNAL_API_KEY=... \
//   TRUE_ADMIN_PASSWORD=... RABBITMQ_PASSWORD=... \
//   az deployment group create -g Store-App -f infra/bicep/main.bicep -p infra/bicep/main.bicepparam
//
// The CD workflow does exactly that with the repository's secrets.

using './main.bicep'

param imageTag = readEnvironmentVariable('IMAGE_TAG')
param postgresHost = readEnvironmentVariable('POSTGRES_HOST')
param postgresUser = readEnvironmentVariable('POSTGRES_USER', 'store_user')
param postgresPassword = readEnvironmentVariable('POSTGRES_PASSWORD')
param jwtSecretKey = readEnvironmentVariable('JWT_SECRET_KEY')
param internalApiKey = readEnvironmentVariable('INTERNAL_API_KEY')
param trueAdminPassword = readEnvironmentVariable('TRUE_ADMIN_PASSWORD')
param rabbitMqPassword = readEnvironmentVariable('RABBITMQ_PASSWORD')
param demoEnabled = bool(readEnvironmentVariable('DEMO_ENABLED', 'false'))
