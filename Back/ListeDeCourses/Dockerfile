FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Api/*.csproj Api/
RUN dotnet restore Api/*.csproj
COPY Api/ Api/
RUN dotnet publish Api/*.csproj -c Release -o /app/out /p:UseAppHost=false /p:AssemblyName=app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENTRYPOINT ["dotnet", "app.dll"]
