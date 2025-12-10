# News Score API

REST API for calculating NEWS (National Early Warning Score) based on patient measurements.

## Requirements
- .NET 9.0
- Docker (recommended) or PostgreSQL 16+

## Setup
### Setting up DB (postgresql)
```bash
docker-compose up -d
```

## Update appsettings
- If you have made any changes to the docker-compose.yml make sure to update connectionString in `appsettings.json`

## Apply migrations to DB (Entity Framework)
- Run migrations: `dotnet ef database update`

## Initial data for DB (Optional)
If you need to fill your db with data, here is a sql file to fill it with test data:

**Using Docker (Bash/Linux/Mac):**
```bash
docker exec -i news-score-api-postgres psql -U postgres -d newsscoreapi < seed_news_score_ranges.sql
```

**Using Docker (PowerShell/Windows):**
```powershell
Get-Content seed_news_score_ranges.sql | docker exec -i news-score-api-postgres psql -U postgres -d newsscoreapi
```

**Using psql directly:**
```bash
psql -U postgres -d newsscoreapi -f seed_news_score_ranges.sql
```

Alternatively, you can create ranges using the API endpoints.

## Run application
- From commandline `dotnet run`

## Endpoints

### Calculate NEWS Score
- **POST** `/api/newsscore/calculate`
- Request body: List of measurements (TEMP, HR, RR)
- Returns: Calculated score

### Example request body:

```
{
  "measurements": [
    {
      "type": "TEMP",
      "value": 37.5
    },
    {
      "type": "HR",
      "value": 60
    },
    {
      "type": "RR",
      "value": 5
    }
  ]
}
```

### Manage Score Ranges
- **GET** `/api/newsscore/ranges` - Get all ranges
    - Optional query to get a specific measurement : `?measurementType=TEMP`
- **POST** `/api/newsscore/ranges` - Create new ranges
- **DELETE** `/api/newsscore/ranges` - Delete ranges
- **PUT** `/api/newsscore/ranges/{id}` - Update range
- **DELETE** `/api/newsscore/ranges/{id}` - Delete range

### Health Check
- **GET** `/health` - Check API and database health

## API Specifications

- Swagger UI: `http://localhost:{port}/swagger` 
- OpenAPI spec: `http://localhost:{port}/swagger/v1/swagger.json`


## Testing

Run unit tests:
```
dotnet test
```


