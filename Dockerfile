# Stage 1: build the frontend
FROM node:24-alpine AS client-build
WORKDIR /src/client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

# Stage 2: build the server
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server-build
WORKDIR /src
COPY server/TypePulse.csproj server/
RUN dotnet restore server/TypePulse.csproj
COPY server/ server/
COPY --from=client-build /src/server/wwwroot server/wwwroot
RUN dotnet publish server/TypePulse.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=server-build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TypePulse.dll"]