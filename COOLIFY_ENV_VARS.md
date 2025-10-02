# Required Environment Variables for Coolify

Set these in your Coolify application settings:

## Critical Variables (MUST SET)

```bash
# JWT Secret - Generate a secure random string
JWT_SECRET_KEY=your_very_long_and_secure_random_string_minimum_32_characters_required

# CORS - Allow frontend to access backend
CORS_ALLOWED_ORIGINS=https://notion.dutchcaz.com

# Database Password
POSTGRES_PASSWORD=your_secure_database_password_here

# MinIO Credentials
MINIO_ROOT_USER=minioadmin
MINIO_ROOT_PASSWORD=your_secure_minio_password_here
```

## Optional Variables (Have Defaults)

```bash
# Database
POSTGRES_DB=notionclone
POSTGRES_USER=postgres

# JWT Settings
JWT_ISSUER=NotionClone
JWT_AUDIENCE=NotionCloneUsers
JWT_EXPIRY_MINUTES=60

# MinIO
MINIO_BUCKET_NAME=notionclone-uploads

# Ports (usually not needed in Coolify)
BACKEND_PORT=8081
FRONTEND_PORT=3000
```

## After Setting Variables

1. Save the environment variables in Coolify
2. Redeploy the application
3. Check backend logs to verify CORS origins are loaded:
   - Look for: "Configured CORS origins: https://notion.dutchcaz.com"
4. Test the frontend at https://notion.dutchcaz.com
