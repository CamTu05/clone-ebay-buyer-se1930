FROM node:22-alpine AS build
WORKDIR /app
ARG VITE_API_BASE_URL=
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
COPY frontend/buyer-web/package*.json ./
RUN npm ci
COPY frontend/buyer-web/ .
RUN npm run build

FROM nginx:1.27-alpine
COPY infrastructure/nginx/frontend.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
