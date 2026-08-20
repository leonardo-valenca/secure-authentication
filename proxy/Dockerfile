# Build context is the repo root (see docker-compose.yml) so this can reach src/frontend.
FROM node:26-alpine AS build
WORKDIR /src
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci
COPY src/frontend/ ./
RUN npx ng build

FROM caddy:2-alpine
COPY proxy/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /src/dist/frontend/browser /srv
