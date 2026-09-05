FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/CloudCure.Domain/CloudCure.Domain.csproj src/CloudCure.Domain/
COPY src/CloudCure.Infrastructure/CloudCure.Infrastructure.csproj src/CloudCure.Infrastructure/
COPY src/CloudCure.Application/CloudCure.Application.csproj src/CloudCure.Application/
COPY src/CloudCure.Api/CloudCure.Api.csproj src/CloudCure.Api/
RUN dotnet restore src/CloudCure.Api/CloudCure.Api.csproj

COPY src/ src/
RUN dotnet publish src/CloudCure.Api/CloudCure.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd -r cloudcure && useradd -r -g cloudcure cloudcure
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER cloudcure
ENTRYPOINT ["dotnet", "CloudCure.Api.dll"]
