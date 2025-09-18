
# Build stage
FROM node:20-alpine AS builder

WORKDIR /app

# Copy package files
COPY package*.json ./


# Install all dependencies (including devDependencies)
RUN npm ci

# Copy source code
COPY . .


# Build the application
RUN npm run build

# Remove devDependencies after build to reduce image size (optional)
RUN npm prune --production

# Production stage
FROM nginx:alpine AS production

# Copy built application
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Ensure mime.types is present (fixes JS MIME type issue)
RUN [ -f /etc/nginx/mime.types ] || wget -O /etc/nginx/mime.types https://raw.githubusercontent.com/nginx/nginx/master/conf/mime.types

# Expose port
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
