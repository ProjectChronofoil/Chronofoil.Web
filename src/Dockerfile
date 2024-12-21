FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /build
COPY ../src src
COPY ../deps deps
RUN dotnet publish "src/Chronofoil.Web.csproj" -c Release -o /app/publish

FROM base AS final
ENV TZ=America/New_York
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Chronofoil.Web.dll"]
