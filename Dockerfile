FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src

COPY DebuggingDemo.csproj ./
RUN dotnet restore DebuggingDemo.csproj

COPY . ./
RUN dotnet publish DebuggingDemo.csproj \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:DebugType=portable \
    /p:Optimize=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl unzip \
    && curl --silent --show-error --location https://aka.ms/getvsdbgsh \
       | /bin/sh /dev/stdin -v latest -l /vsdbg \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DebuggingDemo.dll"]
