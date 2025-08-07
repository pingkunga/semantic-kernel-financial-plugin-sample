# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY . .
# Restore as distinct layer
RUN dotnet restore

WORKDIR /src/FinancialChatBotBlazerUI
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
# Serve Stage
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
COPY /FinancialChatBotBlazerUI/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]