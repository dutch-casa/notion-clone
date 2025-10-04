# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY Backend.Domain/Backend.Domain.csproj Backend.Domain/
COPY Backend.Application/Backend.Application.csproj Backend.Application/
COPY Backend.Infrastructure/Backend.Infrastructure.csproj Backend.Infrastructure/
COPY Backend.Presentation/Backend.Presentation.csproj Backend.Presentation/
RUN dotnet restore Backend.Presentation/Backend.Presentation.csproj

# Copy source code and build
COPY . .
WORKDIR /src/Backend.Presentation
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage - using SDK image to run migrations
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS final
WORKDIR /app

# Install dependencies
RUN apk add --no-cache icu-libs

# Copy published app
COPY --from=build /app/publish .

# Copy source files needed for migrations
COPY --from=build /src /src

# Build the projects in /src so --no-build works for EF migrations
WORKDIR /src
RUN dotnet restore Backend.Presentation/Backend.Presentation.csproj && \
    dotnet build Backend.Presentation/Backend.Presentation.csproj -c Release

# Create non-root user for security
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser

# Create startup script that runs migrations then starts the app
WORKDIR /app
RUN echo '#!/bin/sh' > /app/docker-entrypoint.sh && \
    echo 'set -e' >> /app/docker-entrypoint.sh && \
    echo '' >> /app/docker-entrypoint.sh && \
    echo 'echo "DEBUG: ConnectionString=$ConnectionStrings__DefaultConnection"' >> /app/docker-entrypoint.sh && \
    echo 'echo "DEBUG: ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT"' >> /app/docker-entrypoint.sh && \
    echo 'echo "Running database migrations..."' >> /app/docker-entrypoint.sh && \
    echo 'cd /src' >> /app/docker-entrypoint.sh && \
    echo 'dotnet ef database update --project Backend.Infrastructure/Backend.Infrastructure.csproj --startup-project Backend.Presentation/Backend.Presentation.csproj --configuration Release --no-build' >> /app/docker-entrypoint.sh && \
    echo 'echo "Migrations completed"' >> /app/docker-entrypoint.sh && \
    echo '' >> /app/docker-entrypoint.sh && \
    echo 'cd /app' >> /app/docker-entrypoint.sh && \
    echo 'echo "Starting application..."' >> /app/docker-entrypoint.sh && \
    echo 'exec dotnet Backend.Presentation.dll "$@"' >> /app/docker-entrypoint.sh && \
    chmod +x /app/docker-entrypoint.sh

# Set ownership
RUN chown -R appuser:appuser /app /src

# Switch to non-root user to install dotnet-ef for that user
USER appuser

# Install EF Core tools globally for appuser
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/home/appuser/.dotnet/tools"

# Expose port
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:8081/health || exit 1

# Run application with migrations
ENTRYPOINT ["/app/docker-entrypoint.sh"]
