FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY portal-gui.csproj .
RUN dotnet restore portal-gui.csproj
COPY . .
RUN dotnet build portal-gui.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish portal-gui.csproj -c Release -o /app/publish

FROM nginx AS final
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf