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

# Create non-root user for security
RUN addgroup -g 1000 appuser && adduser -D -u 1000 -G appuser appuser

# Copy published app
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "Backend.Presentation.dll"]
