FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
EXPOSE 8080

ENTRYPOINT ["dotnet", "watch", "run", "/app/src/Chronofoil.Web.csproj"]