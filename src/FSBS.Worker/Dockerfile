FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish src/FSBS.Worker/FSBS.Worker.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system --gid 1001 appgroup \
 && useradd --system --uid 1001 --gid appgroup appuser

COPY --from=build /app/publish .

USER appuser

ENTRYPOINT ["dotnet", "FSBS.Worker.dll"]
