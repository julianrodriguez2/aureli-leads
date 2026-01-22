# aureli-leads

Lead management and automation monorepo.

## Apps
- apps/api/AureliLeads.Api: ASP.NET Core Web API
- apps/web: Next.js App Router frontend

## Docker
docker-compose up --build

## Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

## Notes
- Update Jwt__Key and database credentials for production.
- Default admin user (seeded on first run): admin@local.test / Admin123!
