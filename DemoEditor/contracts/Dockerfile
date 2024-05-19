FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
COPY . /source
WORKDIR /source
RUN dotnet publish "contracts.csproj" -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS final
WORKDIR /app
COPY --from=build /app .
COPY Lato-Regular.ttf Lato-Regular.ttf
COPY Lato-Semibold.ttf Lato-Semibold.ttf
ENTRYPOINT ["dotnet", "contracts.dll"]
