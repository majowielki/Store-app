#!/bin/bash

echo "Building all microservices..."

# Build Shared libraries
echo "Building Shared libraries..."
cd ../../Shared/Store.Shared.DTOs
dotnet build
cd ../Store.Shared.Contracts
dotnet build
cd ../Store.Shared
dotnet build
cd ../../Infrastructure/scripts

# Build microservices
services=("ProductService" "IdentityService" "CartService" "OrderService")

for service in "${services[@]}"
do
    echo "Building $service..."
    cd "../../Services/$service"
    dotnet build
    cd ../../Infrastructure/scripts
done

# Build Gateway
echo "Building Gateway..."
cd ../../Gateway/APIGateway
dotnet build
cd ../../Infrastructure/scripts

echo "All microservices built successfully!"
