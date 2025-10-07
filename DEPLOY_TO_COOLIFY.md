# Deploy Notion Clone to Coolify

Quick guide for deploying this project to your Coolify instance at `72.60.167.245`.

## Prerequisites

1. Coolify installed and accessible
2. Domain names configured:
   - `notion.dutchcaz.com` → Frontend
   - `api.notion.dutchcaz.com` → Backend API

## Deployment Steps

### Step 1: Create New Project in Coolify

1. Log into your Coolify dashboard
2. Create a new project: **"Notion Clone"**
3. Connect your Git repository or use Docker Compose

### Step 2: Deploy via Docker Compose

Coolify can automatically deploy the entire stack using the included `docker-compose.yml`.

1. In Coolify, create a new **Docker Compose** resource
2. Point it to this repository
3. Coolify will automatically detect `docker-compose.yml`

### Step 3: Configure Environment Variables

In Coolify's environment variables section, add these **REQUIRED** variables:

```bash
# Critical - Generate secure values
JWT_SECRET_KEY=<generate-with-openssl-rand-base64-32>
POSTGRES_PASSWORD=<your-secure-db-password>
MINIO_ROOT_PASSWORD=<your-secure-minio-password>

# Your domains
CORS_ALLOWED_ORIGINS=https://notion.dutchcaz.com
VITE_API_BASE_URL=https://api.notion.dutchcaz.com

# Set to Production
ASPNETCORE_ENVIRONMENT=Production
```

**Generate JWT Secret:**
```bash
openssl rand -base64 32
```

### Step 4: Configure Service Domains

Configure each service in Coolify:

#### Frontend Service
- **Port:** 8080
- **Domain:** `notion.dutchcaz.com`
- **Health Check:** `/` (optional)

#### Backend Service
- **Port:** 8081
- **Domain:** `api.notion.dutchcaz.com`
- **Health Check:** `/health`

#### Internal Services (No public domain needed)
- PostgreSQL (port 5432)
- Redis (port 6379)
- MinIO (ports 9000, 9001)

### Step 5: Deploy

1. Click **Deploy** in Coolify
2. Monitor the deployment logs
3. Wait for all services to become healthy (green checkmarks)

### Step 6: Verify Deployment

Test these endpoints:

1. **Backend Health:** https://api.notion.dutchcaz.com/health
   - Should return: `Healthy`

2. **Frontend:** https://notion.dutchcaz.com
   - Should load the application

3. **API Connectivity:** Open browser console on frontend
   - Should see successful API calls
   - No CORS errors

4. **Real-time Features:**
   - Test document collaboration
   - Test notifications

## Troubleshooting

### Build Fails

**Frontend build error?**
- Check that `VITE_API_BASE_URL` is set correctly
- Verify the backend is accessible during build

**Backend build error?**
- Check Docker build logs in Coolify
- Verify .NET 9.0 SDK is available

### Runtime Issues

**CORS Errors?**
```bash
# Verify in Coolify environment:
CORS_ALLOWED_ORIGINS=https://notion.dutchcaz.com
```

**Database Connection Failed?**
- Check PostgreSQL service is running
- Verify `POSTGRES_PASSWORD` matches in all places
- Check docker network connectivity

**MinIO Upload Fails?**
- Verify MinIO service is healthy
- Check MinIO credentials are set correctly
- Ensure minio-init service ran successfully

**WebSocket/SignalR Not Working?**
- Coolify proxy should support WebSocket upgrades (it does by default)
- Check `/hubs/document` endpoint is accessible

### Check Logs

In Coolify, view logs for each service:
1. Click on the service
2. View **Logs** tab
3. Look for error messages

**Common log searches:**
- Backend: `error`, `exception`, `failed`
- Frontend: Check browser console (F12)
- Database: `ERROR`, `FATAL`

## Local Development vs Production

### Local Development

Create a `.env.local` file:
```bash
ASPNETCORE_ENVIRONMENT=Development
CORS_ALLOWED_ORIGINS=http://localhost:3000
VITE_API_BASE_URL=http://localhost:8081
```

Run with:
```bash
docker-compose --env-file .env.local up
```

### Production (Coolify)

Uses `.env` values or Coolify environment variables (Coolify overrides).

## Database Migrations

Migrations run automatically on backend startup. If you need to run them manually:

```bash
# In the backend container
dotnet ef database update
```

In Coolify, you can exec into the backend container and run this command.

## Backup & Restore

### Database Backup

```bash
docker exec -t notion-clone-postgres-1 pg_dump -U postgres notionclone > backup.sql
```

### Database Restore

```bash
docker exec -i notion-clone-postgres-1 psql -U postgres notionclone < backup.sql
```

### MinIO/File Backup

Files are stored in the `minio_data` Docker volume. In Coolify, you can configure volume backups.

## Monitoring

Monitor these in Coolify:

1. **Service Health:** All services should show green/healthy
2. **CPU/Memory:** Watch resource usage
3. **Logs:** Check for errors
4. **Uptime:** Ensure services stay running

## Scaling

If you need to scale:

1. **Database:** Consider managed PostgreSQL (Railway, Neon, Supabase)
2. **Redis:** Consider managed Redis (Upstash, Redis Cloud)
3. **Object Storage:** Consider S3, Cloudflare R2, or Backblaze B2
4. **Backend:** Coolify can run multiple replicas

## Security Checklist

- [ ] JWT_SECRET_KEY is randomly generated (32+ chars)
- [ ] Strong database password
- [ ] Strong MinIO password
- [ ] CORS only allows your domain
- [ ] HTTPS enabled for both frontend and backend
- [ ] Firewall configured (only ports 80, 443 open)
- [ ] Regular backups configured

## Support

**Deployment Issues?**
1. Check Coolify logs
2. Verify all environment variables
3. Test each service health endpoint
4. Check docker container logs

**Application Issues?**
1. Check browser console for frontend errors
2. Check backend logs for API errors
3. Verify database connectivity
4. Test MinIO file access

## Quick Reference

| Service | Port | URL | Health Check |
|---------|------|-----|--------------|
| Frontend | 8080 | https://notion.dutchcaz.com | / |
| Backend | 8081 | https://api.notion.dutchcaz.com | /health |
| PostgreSQL | 5432 | Internal only | - |
| Redis | 6379 | Internal only | - |
| MinIO | 9000 | Internal only | - |
| MinIO Console | 9001 | Optional | - |

## Next Steps After Deployment

1. ✅ Create your first user account
2. ✅ Create an organization
3. ✅ Test creating pages and documents
4. ✅ Test real-time collaboration
5. ✅ Test file uploads
6. ✅ Set up backups
7. ✅ Monitor resource usage

---

**Need help?** Check the Coolify logs and container logs for detailed error messages.
