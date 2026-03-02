# Deployment Guide

## Prerequisites
- Docker & Docker Compose (recommended)
- OR .NET 10 SDK, Node.js 20+, SQL Server 2022

## Option 1: Docker Deployment (Recommended)

1. **Build and run all services:**
```bash
docker-compose up -d
```

2. **Initialize database:**
```bash
# Run SQL scripts in order
docker exec -i enterprise-db sqlcmd -S localhost -U sa -P YourStrong!Password -d master -i SQL_Script/001_CreateDatabase.sql
docker exec -i enterprise-db sqlcmd -S localhost -U sa -P YourStrong!Password -d EnterpriseWeb_DB -i SQL_Script/002_CreateTables.sql
docker exec -i enterprise-db sqlcmd -S localhost -U sa -P YourStrong!Password -d EnterpriseWeb_DB -i SQL_Script/003_SeedData.sql
```

## Option 2: Manual Deployment

### Backend
```bash
cd backend
dotnet publish -c Release -o publish
# Deploy publish/ folder to your server
```

### Frontend
```bash
cd frontend/client-admin
npm run build
# Deploy dist/ folder to web server

cd ../client-web
npm run build
# Deploy dist/ folder to web server
```

## Environment Configuration

### Production Environment Variables
Create `.env` file with:
```env
ConnectionStrings__DefaultConnection=Server=prod-server;Database=EnterpriseWeb_DB;User Id=app_user;Password=strong_password;
Jwt__Key=your_production_jwt_secret_key_minimum_32_characters
Jwt__Issuer=EnterpriseWeb.API
Jwt__Audience=EnterpriseWeb.Clients
Seq__ServerUrl=https://your-seq-server
```

### Security Checklist
- [ ] Replace JWT key with strong secret
- [ ] Use production database with proper credentials
- [ ] Configure HTTPS certificates
- [ ] Set up proper CORS origins
- [ ] Enable rate limiting
- [ ] Configure logging to centralized system
- [ ] Set up backup strategy for database

## Monitoring
- Application logs: Seq (configured via Seq__ServerUrl)
- Database performance: SQL Server Profiler/Query Store
- Application performance: Consider Application Insights or similar

## Scaling
- Backend: Scale horizontally behind load balancer
- Database: Consider read replicas for heavy read workloads
- Frontend: Serve via CDN for static assets
