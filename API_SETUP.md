# File Share Portal API - Setup Guide

## Prerequisites

- SQL Server database
- .NET Framework 4.8
- IIS or Visual Studio for hosting
- NuGet Package Manager

## Installation Steps

### 1. Restore NuGet Packages

The API requires ASP.NET Web API packages. Restore all packages:

```bash
nuget restore FileSharePortal.sln
```

Or in Visual Studio:
- Right-click solution → Restore NuGet Packages

### 2. Run Database Migration

Execute the SQL migration script to add API-related tables:

**File**: `Database/AddApiTables.sql`

```sql
-- Run this script against your FileSharePortal database
-- It will add:
-- - ApiTokens table
-- - ApplicationLogFiles table
-- - API-related columns to Applications and ApplicationExecutions
```

**Using SQL Server Management Studio:**
1. Open SSMS and connect to your database server
2. Open `Database/AddApiTables.sql`
3. Select the FileSharePortal database
4. Execute the script (F5)

**Using command line:**
```bash
sqlcmd -S your-server -d FileSharePortal -i "Database\AddApiTables.sql"
```

### 3. Verify Configuration

**Web.config** should already have the connection string:

```xml
<connectionStrings>
  <add name="FileSharePortalContext"
       connectionString="Data Source=YOUR_SERVER;Initial Catalog=FileSharePortal;Integrated Security=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

### 4. Build the Project

In Visual Studio:
- Build → Build Solution (Ctrl+Shift+B)

Or from command line:
```bash
msbuild FileSharePortal.sln /p:Configuration=Release
```

### 5. Deploy to IIS (Production)

1. **Publish the application:**
   - Right-click project → Publish
   - Choose target location
   - Publish

2. **Configure IIS:**
   - Create new application pool (.NET Framework v4.0, Integrated)
   - Create new website or application
   - Point to published folder
   - Set application pool

3. **Set permissions:**
   - IIS_IUSRS needs read access to application folder
   - Application pool identity needs database access

### 6. Test the API

**Option A: Using PowerShell**

```powershell
# Test authentication endpoint
$body = @{
    username = "admin"
    password = "admin123"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost/api/auth/connect" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"

Write-Host "Token: $($response.token)"
```

**Option B: Using cURL**

```bash
# Test authentication endpoint
curl -X POST http://localhost/api/auth/connect \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

**Option C: Using Postman**

1. Create new request
2. Method: POST
3. URL: `http://localhost/api/auth/connect`
4. Headers: `Content-Type: application/json`
5. Body (raw JSON):
   ```json
   {
     "username": "admin",
     "password": "admin123"
   }
   ```
6. Send

Expected response:
```json
{
  "token": "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnop",
  "expiresDate": "2025-02-01T12:00:00",
  "user": {
    "userId": 1,
    "username": "admin",
    "fullName": "System Administrator",
    "email": "admin@fileshareportal.local",
    "isAdmin": true
  }
}
```

## Registering Your First Application

### Step 1: Get Admin Token

```bash
curl -X POST http://your-server/api/auth/connect \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

Save the returned token.

### Step 2: Register Application

```bash
curl -X POST http://your-server/api/admin/applications/register \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "applicationName": "My Application",
    "description": "Description of my application",
    "contactEmail": "dev@company.com"
  }'
```

**Response:**
```json
{
  "applicationId": 1,
  "applicationName": "My Application",
  "apiKey": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
  "registeredDate": "2025-01-15T10:00:00",
  "message": "Application registered successfully. Please store the API key securely."
}
```

**IMPORTANT:** Save the API key! It cannot be retrieved later.

### Step 3: Test Application Authentication

```bash
curl -X POST http://your-server/api/auth/authenticate \
  -H "Content-Type: application/json" \
  -d '{
    "applicationName": "My Application",
    "apiKey": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6"
  }'
```

## Security Considerations

### 1. HTTPS in Production

**Always use HTTPS in production!** API tokens are sensitive.

In IIS:
1. Install SSL certificate
2. Bind HTTPS (port 443) to your site
3. Enable "Require SSL" in SSL Settings

### 2. Token Storage

**Client Applications:**
- Store tokens securely (encrypted storage, environment variables)
- Never commit tokens to source control
- Rotate tokens regularly

**API Keys:**
- Treat API keys like passwords
- Store in secure configuration (Azure Key Vault, AWS Secrets Manager, etc.)
- Never expose in client-side code

### 3. Database Security

- Use least-privilege database accounts
- Enable SQL Server encryption (TDE)
- Regular backups
- Audit sensitive operations

### 4. CORS (if needed)

If you need to call the API from a web application:

Add to `Web.config`:
```xml
<system.webServer>
  <httpProtocol>
    <customHeaders>
      <add name="Access-Control-Allow-Origin" value="https://your-trusted-domain.com" />
      <add name="Access-Control-Allow-Headers" value="Content-Type,Authorization" />
      <add name="Access-Control-Allow-Methods" value="GET,POST,PUT,DELETE,OPTIONS" />
    </customHeaders>
  </httpProtocol>
</system.webServer>
```

**Warning:** Never use `*` for Access-Control-Allow-Origin in production!

## Troubleshooting

### 404 Not Found on API Endpoints

**Symptom:** API endpoints return 404, but MVC pages work.

**Solution:** Ensure WebApiConfig is registered in Global.asax.cs:

```csharp
protected void Application_Start()
{
    AreaRegistration.RegisterAllAreas();
    GlobalConfiguration.Configure(WebApiConfig.Register); // This line
    FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
    RouteConfig.RegisterRoutes(RouteTable.Routes);
    BundleConfig.RegisterBundles(BundleTable.Bundles);
}
```

### 401 Unauthorized on Valid Token

**Symptom:** Token works in Postman but not in your application.

**Possible Causes:**
1. Token expired (30-day lifetime)
2. Token was revoked
3. User account was deactivated
4. Authorization header format incorrect

**Check Authorization Header:**
```
Authorization: Bearer YOUR_TOKEN_HERE
```

NOT:
- `Bearer: YOUR_TOKEN_HERE`
- `Token YOUR_TOKEN_HERE`
- `YOUR_TOKEN_HERE`

### 500 Internal Server Error

**Symptom:** API returns 500 errors.

**Steps:**
1. Check Windows Event Viewer → Application logs
2. Enable detailed errors in Web.config (dev only!):
   ```xml
   <system.web>
     <customErrors mode="Off"/>
   </system.web>
   ```
3. Check database connection
4. Verify all migrations ran successfully

### Database Connection Issues

**Symptom:** API can't connect to database.

**Check:**
1. Connection string in Web.config
2. SQL Server allows remote connections
3. Firewall allows SQL Server port (1433)
4. Application pool identity has database access

**Grant database access:**
```sql
USE FileSharePortal
GO
CREATE USER [IIS APPPOOL\FileSharePortalAppPool] FOR LOGIN [IIS APPPOOL\FileSharePortalAppPool]
GO
EXEC sp_addrolemember 'db_datareader', 'IIS APPPOOL\FileSharePortalAppPool'
EXEC sp_addrolemember 'db_datawriter', 'IIS APPPOOL\FileSharePortalAppPool'
GO
```

## Performance Tuning

### 1. Enable Response Compression

In Web.config:
```xml
<system.webServer>
  <urlCompression doStaticCompression="true" doDynamicCompression="true" />
</system.webServer>
```

### 2. Connection Pooling

Connection pooling is enabled by default. Verify in connection string:
```
Data Source=SERVER;Initial Catalog=DB;Integrated Security=True;Pooling=True;Max Pool Size=100;
```

### 3. Caching

For read-heavy operations, implement caching in your application:
- Cache file metadata
- Cache user permissions
- Cache application configurations

### 4. Async Operations

The API already uses async/await for file operations. Ensure client applications also use async calls.

## Monitoring

### 1. Application Logs

Log files are stored in:
- Windows Event Viewer → Application
- IIS logs: `C:\inetpub\logs\LogFiles`

### 2. API Usage Tracking

Query ApiTokens table:
```sql
SELECT
    u.Username,
    a.ApplicationName,
    t.LastUsedDate,
    t.CreatedDate,
    t.ExpiresDate,
    t.IsRevoked
FROM ApiTokens t
INNER JOIN Users u ON t.UserId = u.UserId
LEFT JOIN Applications a ON t.ApplicationId = a.ApplicationId
ORDER BY t.LastUsedDate DESC
```

### 3. Application Executions

Monitor application runs:
```sql
SELECT
    a.ApplicationName,
    e.StartTime,
    e.EndTime,
    e.Status,
    e.RecordsProcessed,
    DATEDIFF(SECOND, e.StartTime, COALESCE(e.EndTime, GETDATE())) as DurationSeconds
FROM ApplicationExecutions e
INNER JOIN Applications a ON e.ApplicationId = a.ApplicationId
WHERE e.StartTime >= DATEADD(DAY, -7, GETDATE())
ORDER BY e.StartTime DESC
```

## Next Steps

1. Read the [API Documentation](API_DOCUMENTATION.md)
2. Register your first application
3. Test file upload/download operations
4. Implement error handling in your client
5. Set up monitoring and alerts

## Support

For issues or questions:
1. Check the [API Documentation](API_DOCUMENTATION.md)
2. Review this setup guide
3. Check Windows Event Viewer logs
4. Contact your system administrator
