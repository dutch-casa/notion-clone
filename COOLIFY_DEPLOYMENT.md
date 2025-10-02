# Coolify Deployment Configuration

## Deployment URLs

- **Frontend**: `https://notion.dutchcaz.com`
- **Backend**: `https://api.notion.dutchcaz.com`

## Current Configuration Status

### ✅ What's Already Correct

1. **Frontend Dockerfile** (`frontend/Dockerfile:15-16`)

   - Has `VITE_API_BASE_URL=https://api.notion.dutchcaz.com` as default
   - Will be baked into the build

2. **Backend CORS** (`docker-compose.yml:96`)

   - Defaults to `https://notion.dutchcaz.com`
   - Will allow frontend to connect

3. **Backend URL Configuration**
   - Backend exposes port 8081
   - Health check endpoint at `/health`

### ⚠️ Required Environment Variables for Coolify

You need to set these environment variables in Coolify:

#### Required (Must Set)

```bash
# JWT Configuration (CRITICAL - generate a secure 32+ character secret)
JWT_SECRET_KEY=your_very_long_and_secure_random_string_min_32_chars

# Database
POSTGRES_PASSWORD=your_secure_postgres_password

# MinIO (Object Storage)
MINIO_ROOT_USER=your_minio_admin_user
MINIO_ROOT_PASSWORD=your_secure_minio_password
```

#### Optional (Already have defaults)

```bash
# Frontend/Backend URLs (defaults are correct for your setup)
CORS_ALLOWED_ORIGINS=https://notion.dutchcaz.com

# Database
POSTGRES_DB=notionclone
POSTGRES_USER=postgres

# JWT Settings
JWT_ISSUER=NotionClone
JWT_AUDIENCE=NotionCloneUsers
JWT_EXPIRY_MINUTES=60

# MinIO
MINIO_BUCKET_NAME=notionclone-uploads
```

## Potential Issues to Check

### 1. Frontend Build Args

The `docker-compose.yml` doesn't pass build args to the frontend service. Add this to line 118-120:

```yaml
frontend:
  build:
    context: ./frontend
    dockerfile: Dockerfile
    args:
      VITE_API_BASE_URL: https://api.notion.dutchcaz.com
```

### 2. CORS Configuration for Multiple Origins

If you need to test from localhost too, you need to update the CORS configuration since it only accepts one origin in the current setup. The backend expects an array.

### 3. MinIO External Access

MinIO needs to be accessible from the backend container. In docker-compose, it uses internal networking (`minio:9000`), which is correct. However, if you need external access to MinIO (for direct file uploads), you'll need to expose it through Coolify.

### 4. Coolify Service Configuration

For each service in Coolify:

#### Backend Service

- **Port**: 8081
- **Health Check**: `/health`
- **Domain**: `api.notion.dutchcaz.com`
- **Environment**: Set all required env vars listed above

#### Frontend Service

- **Port**: 8080 (nginx serves on 8080 in container)
- **Health Check**: `/health`
- **Domain**: `notion.dutchcaz.com`

#### Database, Redis, MinIO

- These should be internal services (no public domains)
- They communicate via docker network `notionclone-network`

## Testing Checklist

After deployment:

1. ✅ Backend health check: `https://api.notion.dutchcaz.com/health`
2. ✅ Frontend loads: `https://notion.dutchcaz.com`
3. ✅ API calls from frontend work (check browser console)
4. ✅ WebSocket connection works (check browser console for SignalR)
5. ✅ SSE notifications work (Server-Sent Events for real-time updates)
6. ✅ File uploads work (MinIO)

## Common Issues

### Issue: Frontend can't connect to backend

- **Symptom**: CORS errors in browser console
- **Fix**: Verify `CORS_ALLOWED_ORIGINS` includes `https://notion.dutchcaz.com`
- **Fix**: Verify frontend was built with correct `VITE_API_BASE_URL`

### Issue: WebSocket/SignalR connection fails

- **Symptom**: Real-time collaboration doesn't work
- **Fix**: Ensure Coolify proxy supports WebSocket upgrades
- **Fix**: Check that backend `/hubs/document` endpoint is accessible

### Issue: SSE (Server-Sent Events) connection fails

- **Symptom**: Notifications don't work
- **Fix**: Ensure Coolify proxy supports SSE (long-lived connections)
- **Fix**: Check backend `/api/Pages/stream` endpoint

### Issue: File uploads fail

- **Symptom**: Image uploads don't work
- **Fix**: Verify MinIO is accessible from backend
- **Fix**: Check MinIO bucket was created (minio-init service)
- **Fix**: Verify MinIO credentials are correct

## Security Notes

1. **JWT Secret**: Must be at least 32 characters, randomly generated
2. **Passwords**: Use strong passwords for all services
3. **HTTPS**: Both frontend and backend should use HTTPS in production
4. **Cookies**: Backend uses HttpOnly cookies for JWT tokens (secure)
5. **CORS**: Only allows your frontend domain (good)

## Next Steps

1. Set environment variables in Coolify
2. Update `docker-compose.yml` to pass frontend build args (see Issue #1 above)
3. Deploy and test each checklist item
4. Monitor logs for any errors
