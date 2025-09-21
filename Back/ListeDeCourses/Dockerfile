FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

RUN set -eux; \
    PROJECT_FILE="$(find . -maxdepth 3 -name '*.csproj' | head -n 1)"; \
    echo "Using project: $PROJECT_FILE"; \
    dotnet restore "$PROJECT_FILE"; \
    dotnet publish "$PROJECT_FILE" -c Release -o /app /p:UseAppHost=false /p:AssemblyName=app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "app.dll"]
