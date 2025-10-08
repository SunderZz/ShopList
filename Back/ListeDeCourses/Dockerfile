FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY api/*.csproj api/
RUN dotnet restore api/*.csproj

COPY api/ api/
RUN dotnet publish api/*.csproj -c Release -o /app/out /p:UseAppHost=false /p:AssemblyName=app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "app.dll"]
