# syntax=docker/dockerfile:1
FROM node:24-alpine AS build
WORKDIR /web
COPY src/Web/logistiflow-web/package.json src/Web/logistiflow-web/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY src/Web/logistiflow-web/ ./
RUN npm run build

# Unprivileged nginx: listens on 8080 and runs as a non-root user.
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime
COPY deploy/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --retries=6 CMD wget -qO- http://127.0.0.1:8080/healthz >/dev/null || exit 1
