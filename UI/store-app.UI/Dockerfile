# syntax=docker/dockerfile:1.7
# Build stage: the bundle. Type-check, tests and lint run in CI before an image is built.
FROM node:22-alpine AS builder
WORKDIR /app

COPY package*.json ./
RUN --mount=type=cache,target=/root/.npm,sharing=locked npm ci

COPY . .
RUN npm run build

# Runtime stage: static files behind nginx, API proxied to the gateway, configuration injected
# at container start (docker/40-app-config.sh) - the same image works in every environment.
# The unprivileged variant runs as the "nginx" user and listens on 8080, like the services.
FROM nginxinc/nginx-unprivileged:1.31-alpine AS production

COPY --from=builder --chown=nginx:nginx /app/dist /usr/share/nginx/html
COPY docker/default.conf.template docker/security-headers.inc.template /etc/nginx/templates/
COPY --chmod=755 docker/10-resolver.envsh docker/15-csp.envsh docker/40-app-config.sh /docker-entrypoint.d/

ENV GATEWAY_UPSTREAM=http://gateway:8080 \
    API_BASE_URL=/api/v1

EXPOSE 8080
