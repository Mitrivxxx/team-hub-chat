FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY aspire/TeamHub.ServiceDefaults/TeamHub.ServiceDefaults.csproj aspire/TeamHub.ServiceDefaults/
COPY building-blocks/TeamHub.Observability/TeamHub.Observability.csproj building-blocks/TeamHub.Observability/
COPY building-blocks/TeamHub.BlobStorage/TeamHub.BlobStorage.csproj building-blocks/TeamHub.BlobStorage/
COPY building-blocks/TeamHub.GrpcContracts/TeamHub.GrpcContracts.csproj building-blocks/TeamHub.GrpcContracts/
COPY services/team-hub-chat/team-hub-chat/team-hub-chat.csproj services/team-hub-chat/team-hub-chat/
RUN dotnet restore services/team-hub-chat/team-hub-chat/team-hub-chat.csproj

COPY aspire/TeamHub.ServiceDefaults/ aspire/TeamHub.ServiceDefaults/
COPY building-blocks/TeamHub.Observability/ building-blocks/TeamHub.Observability/
COPY building-blocks/TeamHub.BlobStorage/ building-blocks/TeamHub.BlobStorage/
COPY building-blocks/TeamHub.GrpcContracts/ building-blocks/TeamHub.GrpcContracts/
COPY services/team-hub-chat/team-hub-chat/ services/team-hub-chat/team-hub-chat/
RUN dotnet publish services/team-hub-chat/team-hub-chat/team-hub-chat.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build --chown=app:app /app/publish .

USER app

ENV ASPNETCORE_URLS=
EXPOSE 8080

HEALTHCHECK --interval=120s --timeout=5s --start-period=45s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "team-hub-chat.dll"]
