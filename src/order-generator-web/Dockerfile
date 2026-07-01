# Frontend React + Vite — build estático servido por Nginx
FROM node:20-alpine AS build
WORKDIR /app

COPY src/order-generator-web/package.json src/order-generator-web/package-lock.json ./
RUN npm ci

COPY src/order-generator-web/ ./
RUN npm run build

FROM nginx:alpine AS runtime
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
