# Build stage
FROM node:22-alpine AS builder
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Runtime stage: static files behind nginx, API proxied to the gateway, configuration injected
# at container start (docker/40-app-config.sh) - the same image works in every environment.
FROM nginx:1.27-alpine AS production

COPY --from=builder /app/dist /usr/share/nginx/html
COPY docker/default.conf.template /etc/nginx/templates/default.conf.template
COPY docker/10-resolver.envsh /docker-entrypoint.d/10-resolver.envsh
COPY docker/40-app-config.sh /docker-entrypoint.d/40-app-config.sh
RUN chmod +x /docker-entrypoint.d/10-resolver.envsh /docker-entrypoint.d/40-app-config.sh && rm -f /etc/nginx/conf.d/default.conf

ENV GATEWAY_UPSTREAM=http://gateway:5000 \
    API_BASE_URL=/api \
    USE_AUTH_ME=true

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
