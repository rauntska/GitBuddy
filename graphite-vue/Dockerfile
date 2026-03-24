FROM node:20-alpine AS build

WORKDIR /app

COPY graphite-vue/package*.json ./
RUN npm ci

COPY graphite-vue/ ./
RUN npm run build

FROM nginx:alpine AS production

COPY graphite-vue/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
