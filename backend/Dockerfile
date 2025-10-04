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

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Install dependencies
RUN apk add --no-cache icu-libs

# Copy published app
COPY --from=build /app/publish .

# Create non-root user
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser && \
    chown -R appuser:appuser /app

USER appuser

# Expose port
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:8081/health || exit 1

# Run application (migrations will run in Program.cs)
ENTRYPOINT ["dotnet", "Backend.Presentation.dll"]
