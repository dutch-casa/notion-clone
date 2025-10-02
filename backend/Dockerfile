# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Install EF Core tools for generating migration bundle
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

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

# Generate self-contained migration bundle
WORKDIR /src
RUN dotnet ef migrations bundle \
    --project Backend.Infrastructure/Backend.Infrastructure.csproj \
    --startup-project Backend.Presentation/Backend.Presentation.csproj \
    --configuration Release \
    --self-contained \
    --output /app/publish/efbundle

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Install dependencies for ICU (required for EF Core bundle)
RUN apk add --no-cache icu-libs

# Create non-root user for security
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser

# Copy published app and migration bundle
COPY --from=build /app/publish .

# Create startup script that runs migrations then starts the app
RUN echo '#!/bin/sh' > /app/docker-entrypoint.sh && \
    echo 'set -e' >> /app/docker-entrypoint.sh && \
    echo '' >> /app/docker-entrypoint.sh && \
    echo 'echo "Running database migrations..."' >> /app/docker-entrypoint.sh && \
    echo 'if [ -f /app/efbundle ]; then' >> /app/docker-entrypoint.sh && \
    echo '  ./efbundle --connection "$ConnectionStrings__DefaultConnection" || {' >> /app/docker-entrypoint.sh && \
    echo '    echo "Migration failed! Exiting..."' >> /app/docker-entrypoint.sh && \
    echo '    exit 1' >> /app/docker-entrypoint.sh && \
    echo '  }' >> /app/docker-entrypoint.sh && \
    echo '  echo "Migrations completed successfully"' >> /app/docker-entrypoint.sh && \
    echo 'else' >> /app/docker-entrypoint.sh && \
    echo '  echo "No migration bundle found, skipping migrations"' >> /app/docker-entrypoint.sh && \
    echo 'fi' >> /app/docker-entrypoint.sh && \
    echo '' >> /app/docker-entrypoint.sh && \
    echo 'echo "Starting application..."' >> /app/docker-entrypoint.sh && \
    echo 'exec dotnet Backend.Presentation.dll "$@"' >> /app/docker-entrypoint.sh && \
    chmod +x /app/docker-entrypoint.sh /app/efbundle

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Run application with migrations
ENTRYPOINT ["/app/docker-entrypoint.sh"]
