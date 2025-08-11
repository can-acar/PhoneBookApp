FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl \
	&& rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8085

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ReportService.Api/ReportService.Api.csproj", "ReportService.Api/"]
COPY ["ReportService.ApplicationService/ReportService.ApplicationService.csproj", "ReportService.ApplicationService/"]
COPY ["ReportService.Domain/ReportService.Domain.csproj", "ReportService.Domain/"]
COPY ["ReportService.ApiContract/ReportService.ApiContract.csproj", "ReportService.ApiContract/"]
COPY ["Shared.CrossCutting/Shared.CrossCutting.csproj", "Shared.CrossCutting/"]
COPY ["ReportService.Infrastructure/ReportService.Infrastructure.csproj", "ReportService.Infrastructure/"]
COPY ["ReportService.Container/ReportService.Container.csproj", "ReportService.Container/"]
RUN dotnet restore "ReportService.Api/ReportService.Api.csproj"
COPY . .
WORKDIR "/src/ReportService.Api"
RUN dotnet build "./ReportService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ReportService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN ls -la /app
ENTRYPOINT ["dotnet", "ReportService.Api.dll"]
