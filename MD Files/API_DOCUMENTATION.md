# File Share Portal API Documentation

## Overview

The File Share Portal API provides programmatic access to file management, application monitoring, and administrative functions. This RESTful API uses bearer token authentication and returns JSON responses.

**Base URL**: `http://your-server/api`

---

## Authentication

All API endpoints (except authentication endpoints) require a bearer token in the Authorization header.

### 1. User Authentication

Authenticate with username and password to receive a token.

**Endpoint**: `POST /api/auth/connect`

**Request Body**:
```json
{
  "username": "your-username",
  "password": "your-password"
}
```

**Response** (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresDate": "2025-02-01T12:00:00",
  "user": {
    "userId": 1,
    "username": "john.doe",
    "fullName": "John Doe",
    "email": "john.doe@company.com",
    "isAdmin": false
  }
}
```

**Notes**:
- Active Directory users cannot authenticate via API with password
- Database users only
- Token valid for 30 days

---

### 2. Application Authentication

Authenticate as an application using API key.

**Endpoint**: `POST /api/auth/authenticate`

**Request Body**:
```json
{
  "applicationName": "MyApplication",
  "apiKey": "your-api-key-here"
}
```

**Response** (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresDate": "2025-02-01T12:00:00",
  "applicationId": 5,
  "user": {
    "userId": 10,
    "username": "app.owner",
    "fullName": "Application Owner",
    "email": "owner@company.com",
    "isAdmin": true
  }
}
```

**Notes**:
- Application must be registered by an administrator
- Token is associated with the application owner's account
- All actions performed with this token are attributed to the application

---

### 3. Revoke Token

Revoke the current authentication token.

**Endpoint**: `POST /api/auth/revoke`

**Headers**:
```
Authorization: Bearer {your-token}
```

**Response** (200 OK):
```json
{
  "message": "Token revoked successfully"
}
```

---

## File Management API

All file management endpoints require authentication.

### 1. Get User's Files

Get list of files uploaded by the authenticated user.

**Endpoint**: `GET /api/files`

**Headers**:
```
Authorization: Bearer {your-token}
```

**Response** (200 OK):
```json
[
  {
    "fileId": 1,
    "fileName": "document.pdf",
    "fileSize": 1048576,
    "contentType": "application/pdf",
    "uploadedDate": "2025-01-15T10:30:00",
    "downloadCount": 5,
    "description": "Important document"
  }
]
```

---

### 2. Get Shared Files

Get list of files shared with the authenticated user.

**Endpoint**: `GET /api/files/shared`

**Headers**:
```
Authorization: Bearer {your-token}
```

**Response** (200 OK):
```json
[
  {
    "fileId": 2,
    "fileName": "report.xlsx",
    "fileSize": 2097152,
    "contentType": "application/vnd.ms-excel",
    "uploadedDate": "2025-01-14T09:00:00",
    "uploadedBy": "Jane Smith",
    "downloadCount": 12,
    "description": "Monthly report"
  }
]
```

---

### 3. Upload File

Upload a new file.

**Endpoint**: `POST /api/files/upload`

**Headers**:
```
Authorization: Bearer {your-token}
Content-Type: multipart/form-data
```

**Form Data**:
- `file`: (binary) The file to upload
- `description`: (optional) File description

**Example with cURL**:
```bash
curl -X POST http://your-server/api/files/upload \
  -H "Authorization: Bearer {your-token}" \
  -F "file=@/path/to/file.pdf" \
  -F "description=Important document"
```

**Response** (200 OK):
```json
{
  "fileId": 15,
  "fileName": "document.pdf",
  "fileSize": 1048576,
  "uploadedDate": "2025-01-15T14:30:00",
  "message": "File uploaded successfully"
}
```

---

### 4. Download File

Download a file by ID.

**Endpoint**: `GET /api/files/{fileId}/download`

**Headers**:
```
Authorization: Bearer {your-token}
```

**Response** (200 OK):
- Binary file content
- Content-Type header set to file's MIME type
- Content-Disposition header with filename

**Example with cURL**:
```bash
curl -X GET http://your-server/api/files/15/download \
  -H "Authorization: Bearer {your-token}" \
  -o downloaded-file.pdf
```

---

### 5. Delete File

Delete a file (owner or admin only).

**Endpoint**: `DELETE /api/files/{fileId}`

**Headers**:
```
Authorization: Bearer {your-token}
```

**Response** (200 OK):
```json
{
  "message": "File deleted successfully"
}
```

---

### 6. Share File

Share a file with users and/or roles.

**Endpoint**: `POST /api/files/{fileId}/share`

**Headers**:
```
Authorization: Bearer {your-token}
Content-Type: application/json
```

**Request Body**:
```json
{
  "userIds": [5, 8, 12],
  "roleIds": [2, 3]
}
```

**Notes**:
- Replaces existing shares (not additive)
- Users who gain access receive a notification
- Users who lose access receive a notification
- Role members automatically get access

**Response** (200 OK):
```json
{
  "message": "File shared successfully",
  "usersGainedAccess": 15,
  "usersLostAccess": 3
}
```

---

### 7. Remove Access

Remove access to a file for specific users/roles.

**Endpoint**: `POST /api/files/{fileId}/remove-access`

**Headers**:
```
Authorization: Bearer {your-token}
Content-Type: application/json
```

**Request Body**:
```json
{
  "userIds": [5, 8],
  "roleIds": [2]
}
```

**Response** (200 OK):
```json
{
  "message": "Access removed successfully",
  "usersAffected": 18
}
```

---

## Admin API

All admin endpoints require administrator privileges.

### 1. Synchronize Active Directory

Trigger AD user synchronization.

**Endpoint**: `POST /api/admin/sync-ad`

**Headers**:
```
Authorization: Bearer {your-admin-token}
```

**Response** (200 OK):
```json
{
  "message": "AD synchronization completed successfully",
  "usersAdded": 5,
  "usersUpdated": 12,
  "usersReactivated": 2,
  "usersDisabled": 3
}
```

---

### 2. Register Application

Register a new application for API access.

**Endpoint**: `POST /api/admin/applications/register`

**Headers**:
```
Authorization: Bearer {your-admin-token}
Content-Type: application/json
```

**Request Body**:
```json
{
  "applicationName": "Data Sync Service",
  "description": "Syncs data between systems",
  "contactEmail": "dev-team@company.com"
}
```

**Response** (200 OK):
```json
{
  "applicationId": 5,
  "applicationName": "Data Sync Service",
  "apiKey": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
  "registeredDate": "2025-01-15T10:00:00",
  "message": "Application registered successfully. Please store the API key securely."
}
```

**IMPORTANT**: Store the API key securely. It cannot be retrieved later.

---

### 3. Get Applications

Get list of all registered applications.

**Endpoint**: `GET /api/admin/applications`

**Headers**:
```
Authorization: Bearer {your-admin-token}
```

**Response** (200 OK):
```json
[
  {
    "applicationId": 5,
    "applicationName": "Data Sync Service",
    "description": "Syncs data between systems",
    "contactEmail": "dev-team@company.com",
    "registeredDate": "2025-01-15T10:00:00",
    "isActive": true,
    "currentStatus": "Running",
    "lastSuccessfulRun": "2025-01-15T14:00:00",
    "lastStatusCheck": "2025-01-15T14:05:00"
  }
]
```

---

### 4. Update Last Successful Run

Update the last successful run timestamp for an application.

**Endpoint**: `PUT /api/admin/applications/{applicationId}/last-run`

**Headers**:
```
Authorization: Bearer {your-admin-token}
Content-Type: application/json
```

**Request Body**:
```json
{
  "lastRunDate": "2025-01-15T14:30:00"
}
```

**Notes**: If `lastRunDate` is omitted, current timestamp is used.

**Response** (200 OK):
```json
{
  "applicationId": 5,
  "applicationName": "Data Sync Service",
  "lastSuccessfulRun": "2025-01-15T14:30:00",
  "message": "Last successful run updated"
}
```

---

### 5. Create Execution Record

Create a new execution record for an application.

**Endpoint**: `POST /api/admin/applications/{applicationId}/executions`

**Headers**:
```
Authorization: Bearer {your-admin-token}
Content-Type: application/json
```

**Request Body**:
```json
{
  "status": "Running",
  "statusMessage": "Processing batch 1 of 5"
}
```

**Status Values**: Running, Success, Failed, Error

**Response** (200 OK):
```json
{
  "executionId": 123,
  "applicationId": 5,
  "startTime": "2025-01-15T15:00:00",
  "status": "Running",
  "message": "Execution record created"
}
```

---

### 6. Update Execution

Update an existing execution record.

**Endpoint**: `PUT /api/admin/executions/{executionId}`

**Headers**:
```
Authorization: Bearer {your-admin-token}
Content-Type: application/json
```

**Request Body**:
```json
{
  "status": "Success",
  "statusMessage": "Completed successfully",
  "endTime": "2025-01-15T15:30:00",
  "recordsProcessed": 1500
}
```

**Response** (200 OK):
```json
{
  "executionId": 123,
  "status": "Success",
  "endTime": "2025-01-15T15:30:00",
  "message": "Execution updated successfully"
}
```

**Note**: When status changes to "Success", the application's lastSuccessfulRun is automatically updated.

---

### 7. Upload Log File

Upload a log file for an execution.

**Endpoint**: `POST /api/admin/executions/{executionId}/logs`

**Headers**:
```
Authorization: Bearer {your-admin-token}
Content-Type: multipart/form-data
```

**Form Data**:
- `file`: (binary) The log file
- `description`: (optional) Log file description

**Example with cURL**:
```bash
curl -X POST http://your-server/api/admin/executions/123/logs \
  -H "Authorization: Bearer {your-token}" \
  -F "file=@/path/to/application.log" \
  -F "description=Execution log for batch process"
```

**Response** (200 OK):
```json
{
  "logFileId": 45,
  "fileName": "application.log",
  "fileSize": 524288,
  "uploadedDate": "2025-01-15T15:35:00",
  "message": "Log file uploaded successfully"
}
```

**Notes**:
- Log files are stored in the database (not filesystem)
- Multiple log files can be attached to one execution
- Maximum recommended size: 10MB per file

---

### 8. Get Execution Logs

Get list of log files for an execution.

**Endpoint**: `GET /api/admin/executions/{executionId}/logs`

**Headers**:
```
Authorization: Bearer {your-admin-token}
```

**Response** (200 OK):
```json
[
  {
    "logFileId": 45,
    "fileName": "application.log",
    "fileSize": 524288,
    "contentType": "text/plain",
    "uploadedDate": "2025-01-15T15:35:00",
    "description": "Execution log for batch process"
  }
]
```

---

### 9. Download Log File

Download a log file by ID.

**Endpoint**: `GET /api/admin/logs/{logFileId}/download`

**Headers**:
```
Authorization: Bearer {your-admin-token}
```

**Response** (200 OK):
- Binary file content
- Content-Type header set to file's MIME type
- Content-Disposition header with filename

---

### 10. Get Application Executions

Get execution history for an application.

**Endpoint**: `GET /api/admin/applications/{applicationId}/executions?limit=50`

**Headers**:
```
Authorization: Bearer {your-admin-token}
```

**Query Parameters**:
- `limit` (optional): Number of records to return (default: 50)

**Response** (200 OK):
```json
[
  {
    "executionId": 123,
    "startTime": "2025-01-15T15:00:00",
    "endTime": "2025-01-15T15:30:00",
    "status": "Success",
    "statusMessage": "Completed successfully",
    "recordsProcessed": 1500,
    "executedBy": "John Doe",
    "logFileCount": 2
  }
]
```

---

## Error Responses

All endpoints may return the following error responses:

### 400 Bad Request
```json
{
  "error": "Username and password are required"
}
```

### 401 Unauthorized
```json
{
  "error": "Invalid token"
}
```

### 403 Forbidden
```json
{
  "error": "This action requires administrator privileges"
}
```

### 404 Not Found
```json
{
  "error": "File not found"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error has occurred.",
  "exceptionMessage": "Detailed error message",
  "exceptionType": "System.Exception"
}
```

---

## Complete Example: Application Workflow

Here's a complete example of an application using the API to upload logs:

```python
import requests
import json

# Configuration
BASE_URL = "http://your-server/api"
APP_NAME = "Data Sync Service"
API_KEY = "your-api-key-here"

# 1. Authenticate
auth_response = requests.post(
    f"{BASE_URL}/auth/authenticate",
    json={"applicationName": APP_NAME, "apiKey": API_KEY}
)
auth_data = auth_response.json()
token = auth_data["token"]
app_id = auth_data["applicationId"]

# Set headers for subsequent requests
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

# 2. Create execution record
exec_response = requests.post(
    f"{BASE_URL}/admin/applications/{app_id}/executions",
    headers=headers,
    json={"status": "Running", "statusMessage": "Starting batch process"}
)
execution_id = exec_response.json()["executionId"]

# 3. Perform your application logic
try:
    # ... your application code here ...
    records_processed = 1500
    status = "Success"
    message = "Completed successfully"
except Exception as e:
    records_processed = 0
    status = "Error"
    message = str(e)

# 4. Update execution status
requests.put(
    f"{BASE_URL}/admin/executions/{execution_id}",
    headers=headers,
    json={
        "status": status,
        "statusMessage": message,
        "recordsProcessed": records_processed
    }
)

# 5. Upload log file
with open("application.log", "rb") as log_file:
    files = {"file": log_file}
    requests.post(
        f"{BASE_URL}/admin/executions/{execution_id}/logs",
        headers={"Authorization": f"Bearer {token}"},
        files=files
    )

print("Application execution completed and logged successfully!")
```

---

## Rate Limiting

Currently, there are no rate limits enforced. This may change in future versions.

---

## API Versioning

The current API version is **v1**. The version is implicit in the base URL. Future versions will be available at `/api/v2`, etc.

---

## Support

For API support, contact your system administrator or open an issue in the project repository.
