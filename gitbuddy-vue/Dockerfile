FROM node:20-alpine AS build

WORKDIR /app

COPY gitbuddy-vue/package*.json ./
RUN npm ci

COPY gitbuddy-vue/ ./
RUN npm run build

FROM nginx:alpine AS production

COPY gitbuddy-vue/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
