FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl \
	&& rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NotificationService.Api/NotificationService.Api.csproj", "NotificationService.Api/"]
COPY ["NotificationService.ApiContract/NotificationService.ApiContract.csproj", "NotificationService.ApiContract/"]
COPY ["NotificationService.Domain/NotificationService.Domain.csproj", "NotificationService.Domain/"]
COPY ["NotificationService.ApplicationService/NotificationService.ApplicationService.csproj", "NotificationService.ApplicationService/"]
COPY ["ReportService.Domain/ReportService.Domain.csproj", "ReportService.Domain/"]
COPY ["Shared.CrossCutting/Shared.CrossCutting.csproj", "Shared.CrossCutting/"]
COPY ["NotificationService.Container/NotificationService.Container.csproj", "NotificationService.Container/"]
COPY ["NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "NotificationService.Infrastructure/"]
RUN dotnet restore "NotificationService.Api/NotificationService.Api.csproj"
COPY . .
WORKDIR "/src/NotificationService.Api"
RUN dotnet build "./NotificationService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NotificationService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN ls -la /app
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]
