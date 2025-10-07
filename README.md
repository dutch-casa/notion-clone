# Notion Clone

A full-stack Notion-like collaborative document editor built with Clean Architecture and Domain-Driven Design principles.

## Tech Stack

**Backend:**
- .NET 9.0 (Clean Architecture + DDD)
- Entity Framework Core + PostgreSQL
- Redis (for caching and real-time state)
- MinIO (S3-compatible object storage)
- SignalR (WebSocket real-time collaboration)
- JWT Authentication

**Frontend:**
- React + TypeScript
- Vite
- TanStack Query (React Query)
- Tailwind CSS + shadcn/ui
- Tiptap Editor
- Zustand (state management)

## Quick Start

### Local Development

1. **Clone and setup:**
```bash
git clone <your-repo>
cd notion-clone
cp .env.example .env
```

2. **Update `.env` for local development:**
```bash
CORS_ALLOWED_ORIGINS=http://localhost:3000
VITE_API_BASE_URL=http://localhost:8081
ASPNETCORE_ENVIRONMENT=Development
```

3. **Start all services:**
```bash
docker-compose up -d
```

4. **Access the application:**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:8081
   - API Docs: http://localhost:8081/swagger
   - MinIO Console: http://localhost:9001

### Production Deployment (Coolify)

See **[DEPLOY_TO_COOLIFY.md](./DEPLOY_TO_COOLIFY.md)** for complete deployment instructions.

**Quick Steps:**
1. Push to your Git repository
2. Connect repository to Coolify
3. Set environment variables in Coolify
4. Deploy!

## Project Structure

```
notion-clone/
├── backend/                    # .NET backend
│   ├── Backend.Domain/        # Domain entities and interfaces
│   ├── Backend.Application/   # Business logic and use cases
│   ├── Backend.Infrastructure/ # Data access and external services
│   └── Backend.Presentation/  # API controllers and endpoints
├── frontend/                   # React frontend
│   ├── src/
│   │   ├── components/        # Reusable UI components
│   │   ├── features/          # Feature-specific components
│   │   ├── hooks/             # Custom React hooks
│   │   ├── lib/               # API client and utilities
│   │   └── routes/            # Route components
├── docker-compose.yml         # Multi-service orchestration
└── DEPLOY_TO_COOLIFY.md      # Deployment guide
```

## Features

✅ User authentication (JWT)
✅ Organization/workspace management
✅ Real-time collaborative editing
✅ Document hierarchy (pages and subpages)
✅ Rich text editing (Tiptap)
✅ Image uploads (MinIO)
✅ Real-time notifications (SSE)
✅ Member invitations
✅ Role-based access control

## Development

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project Backend.Presentation
```

### Frontend

```bash
cd frontend
bun install
bun run dev
```

### Generate TypeScript Types from Backend

After making backend changes:

```bash
cd frontend
bun run generate:types
```

## Architecture

This project follows **Clean Architecture** principles with **Domain-Driven Design**:

- **Domain Layer**: Core business entities and logic
- **Application Layer**: Use cases and application-specific business rules
- **Infrastructure Layer**: External concerns (database, file storage, etc.)
- **Presentation Layer**: API endpoints and HTTP concerns

## Database Migrations

Migrations run automatically on startup. To create new migrations:

```bash
cd backend
dotnet ef migrations add YourMigrationName --project Backend.Infrastructure --startup-project Backend.Presentation
```

## Environment Variables

See `.env.example` for all configuration options.

**Critical variables:**
- `JWT_SECRET_KEY` - Must be 32+ characters
- `POSTGRES_PASSWORD` - Secure database password
- `CORS_ALLOWED_ORIGINS` - Your frontend URL
- `VITE_API_BASE_URL` - Backend API URL (for frontend builds)

## Documentation

- [Coolify Deployment Guide](./DEPLOY_TO_COOLIFY.md)
- [Environment Variables Reference](./COOLIFY_ENV_VARS.md)
- [Type Generation Guide](./frontend/TYPE_GENERATION.md)

## Monitoring

**Health Checks:**
- Backend: `/health`
- Frontend: nginx serves on port 8080

**Logs:**
```bash
# View all logs
docker-compose logs -f

# View specific service
docker-compose logs -f backend
```

## Contributing

Built for Auburn Web Dev Club as a demonstration of Clean Architecture and Domain-Driven Design principles.

## License

MIT
