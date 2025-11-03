# File Share Portal

A comprehensive file sharing web application built with ASP.NET MVC (.NET Framework 4.8), featuring user management, role-based access control, notifications, application monitoring, and a modern collapsible sidebar navigation.

## Features

### User Interface
- **Collapsible Sidebar Navigation**: Clean left-side navigation menu that can be hidden/shown
- **Responsive Design**: Fully responsive layout that works on desktop, tablet, and mobile devices
- **Persistent State**: Sidebar state is saved across sessions using localStorage
- **Active Link Highlighting**: Current page is highlighted in the navigation menu
- **Mobile-Friendly**: On mobile devices, sidebar slides in with an overlay
- **Smooth Animations**: All navigation interactions include smooth transitions
- **Modern Design**: Professional Bootstrap 5 interface with custom styling

### 1. File Sharing
- **Upload Files**: Users can upload files up to 100 MB
- **Share with Users**: Share files with individual users
- **Share with Roles**: Share files with groups of users organized by roles
- **Download Tracking**: Monitor how many times files have been downloaded
- **File Descriptions**: Add descriptions to uploaded files
- **Access Control**: Granular control over who can download files

### 2. Notifications System
- **Real-time Notifications**: Stay updated with file sharing activities
- **Multiple Notification Types**:
  - File Shared notifications
  - File Reported alerts
  - File Deleted notifications
  - Role Assignment notifications
  - Application alerts
  - General notifications
- **Unread Badge**: Visual indicator for unread notifications
- **Mark as Read**: Individual or bulk mark as read functionality

### 3. Authentication & Security
- **Dual Authentication Modes**:
  - Active Directory integration
  - Database authentication
- **Secure Password Hashing**: SHA256 password hashing
- **Forms Authentication**: Session management with sliding expiration
- **Anti-Forgery Tokens**: CSRF protection on all forms
- **Authorization Filters**: Role-based access control

### 4. User Management (Admin)
- **Create Users**: Manually add users to the system
- **Toggle Admin Rights**: Grant/revoke administrator privileges
- **Enable/Disable Users**: Activate or deactivate user accounts
- **Track Login History**: Monitor user login activities
- **AD Integration**: Automatic user creation from Active Directory

### 5. Role Management
- **Create Roles**: Define custom roles for organizing users
- **Manual User Assignment**: Add users directly to roles
- **Distribution List Integration**: Link Active Directory distribution lists to roles
- **Dynamic Role Calculation**: Roles automatically include users from all assigned distribution lists
- **Role-based File Sharing**: Share files with entire roles at once

### 6. File Reporting
- **Report Files**: Users can report inappropriate or problematic files
- **Admin Review**: Administrators receive notifications and can review reports
- **Status Tracking**: Track report status (Pending, Under Review, Resolved, Dismissed)
- **Admin Notes**: Document review decisions
- **Quick Actions**: Delete reported files directly from review interface

### 7. Application Monitoring
- **Application Dashboard**: Monitor status of multiple applications
- **Health Checks**: Check application status via HTTP endpoints
- **Execution History**: Track application runs and performance
- **Log Download**: Download application logs directly from the interface
- **Status Indicators**: Visual indicators for Running, Stopped, Error, and Unknown states
- **Auto-refresh**: Dashboard automatically refreshes every 60 seconds

## Technology Stack

- **Framework**: ASP.NET MVC 5.2.7
- **Target Framework**: .NET Framework 4.8
- **ORM**: Entity Framework 6.4.4
- **Database**: SQL Server (LocalDB for development)
- **Frontend**:
  - Bootstrap 5.1.3
  - jQuery 3.6.0
  - Bootstrap Icons
- **Authentication**: Forms Authentication + Active Directory support

## Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.8 SDK
- SQL Server 2016 or later (or SQL Server LocalDB)
- IIS Express (included with Visual Studio)

## Installation & Setup

### 1. Database Configuration

The application uses SQL Server LocalDB by default. To change the database:

1. Open `Web.config`
2. Modify the connection string under `<connectionStrings>`:

```xml
<add name="FileSharePortalContext"
     connectionString="YOUR_CONNECTION_STRING"
     providerName="System.Data.SqlClient" />
```

### 2. Active Directory Configuration (Optional)

To enable Active Directory authentication:

1. Open `Web.config`
2. Update the following settings:

```xml
<add key="UseActiveDirectory" value="true" />
<add key="ADDomain" value="YOUR_DOMAIN" />
```

### 3. File Upload Configuration

Configure file upload settings in `Web.config`:

```xml
<add key="FileUploadPath" value="~/App_Data/Uploads" />
<add key="MaxFileSize" value="104857600" /><!-- 100 MB in bytes -->
```

### 4. Build and Run

1. Open the solution in Visual Studio
2. Restore NuGet packages:
   - Right-click solution → Restore NuGet Packages
3. Build the solution (Ctrl+Shift+B)
4. Run the application (F5)

### 5. Initial Database Setup

The database will be created automatically on first run. The default admin account is:
- **Username**: admin
- **Password**: admin123

**⚠️ IMPORTANT**: Change the default admin password immediately after first login!

## Project Structure

```
FileSharePortal/
├── App_Data/                  # File storage and database
├── App_Start/                 # Configuration files
│   ├── BundleConfig.cs
│   ├── FilterConfig.cs
│   └── RouteConfig.cs
├── Content/                   # CSS files
│   ├── bootstrap.css
│   └── site.css
├── Controllers/               # MVC Controllers
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── ApplicationsController.cs
│   ├── FilesController.cs
│   ├── HomeController.cs
│   └── NotificationsController.cs
├── Data/                      # Database Context
│   └── FileSharePortalContext.cs
├── Models/                    # Entity Models
│   ├── User.cs
│   ├── Role.cs
│   ├── SharedFile.cs
│   ├── FileShare.cs
│   ├── Notification.cs
│   ├── FileReport.cs
│   ├── Application.cs
│   └── [other models]
├── Services/                  # Business Logic
│   ├── AuthenticationService.cs
│   ├── NotificationService.cs
│   └── RoleService.cs
├── Views/                     # Razor Views
│   ├── Account/
│   ├── Admin/
│   ├── Applications/
│   ├── Files/
│   ├── Home/
│   ├── Notifications/
│   └── Shared/
└── Web.config                 # Configuration
```

## Usage Guide

### Navigation

#### Using the Sidebar Menu
- **Toggle Menu**: Click the hamburger icon (☰) in the top-left corner to show/hide the sidebar
- **Desktop**: The sidebar can be collapsed to give more screen space. Your preference is automatically saved
- **Mobile**: The sidebar slides in from the left with a dark overlay. Tap outside to close
- **Active Links**: The current page is highlighted in blue in the sidebar
- **Quick Access**: All main features are accessible from the sidebar:
  - Home dashboard
  - My Files
  - Shared With Me
  - Upload File
  - Applications
  - Admin sections (for administrators)

### For Regular Users

#### Uploading Files
1. Navigate to "My Files" or click "Upload File" from the dashboard
2. Select a file (max 100 MB)
3. Optionally add a description
4. Click "Upload File"

#### Sharing Files
1. Go to "My Files"
2. Click the "Share" button next to the file
3. Select users or roles to share with
4. Click "Share File"

#### Reporting Files
1. Navigate to the file details page
2. Click "Report File"
3. Select a reason and provide details
4. Submit the report (admins will be notified)

### For Administrators

#### Creating Users
1. Go to Admin → Users
2. Click "Create User"
3. Fill in user details
4. Optionally grant admin privileges
5. Submit

#### Managing Roles
1. Go to Admin → Roles
2. Click "Create Role" or select an existing role
3. Add users manually or link distribution lists
4. Users in distribution lists are automatically included

#### Reviewing Reports
1. Go to Admin → File Reports
2. Click "Review" on a report
3. View details and update status
4. Optionally delete the file
5. Add admin notes
6. Submit review

#### Adding Applications for Monitoring
1. Go to Applications
2. Click "Add Application"
3. Enter application details and health check endpoint
4. Configure check interval
5. Submit

## Security Considerations

### Authentication
- Passwords are hashed using SHA256
- Support for Active Directory integration
- Session timeout after 8 hours of inactivity

### Authorization
- All controllers require authentication by default
- Admin-only sections protected with authorization checks
- File access validated based on sharing permissions

### File Upload Security
- File size limits enforced (100 MB default)
- Files stored outside web root in App_Data
- Content-Type validation
- Download access control

### CSRF Protection
- All forms include anti-forgery tokens
- POST requests validated for CSRF attacks

## Configuration Reference

### Web.config Settings

| Setting | Description | Default |
|---------|-------------|---------|
| UseActiveDirectory | Enable/disable AD authentication | false |
| ADDomain | Active Directory domain name | YOURDOMAIN |
| FileUploadPath | Path for uploaded files | ~/App_Data/Uploads |
| MaxFileSize | Maximum file size in bytes | 104857600 (100 MB) |

### Database Connection String

```xml
<connectionStrings>
  <add name="FileSharePortalContext"
       connectionString="Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=FileSharePortal;Integrated Security=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

## Troubleshooting

### Database Connection Issues
- Ensure SQL Server/LocalDB is running
- Verify connection string in Web.config
- Check Windows Authentication permissions

### File Upload Failures
- Check App_Data/Uploads folder exists and has write permissions
- Verify file size is within limits
- Check IIS request size limits

### Active Directory Issues
- Verify domain name is correct
- Ensure application has network access to domain controller
- Check service account permissions

### Performance Issues
- Enable compression in IIS
- Optimize database indexes
- Consider moving file storage to separate drive

## Database Schema

The application uses the following main entities:

- **Users**: User accounts and authentication
- **Roles**: User groups and collections
- **RoleUsers**: Many-to-many relationship between roles and users
- **DistributionLists**: Active Directory distribution list references
- **RoleDistributionLists**: Links between roles and distribution lists
- **SharedFiles**: Uploaded file metadata
- **FileShares**: Sharing permissions (user or role based)
- **Notifications**: User notifications
- **FileReports**: File reports submitted by users
- **Applications**: Monitored applications
- **ApplicationExecutions**: Application execution history

## API Endpoints

### Notifications
- `GET /Notifications/GetUnreadCount` - Get unread notification count
- `POST /Notifications/MarkAsRead` - Mark notification as read
- `POST /Notifications/MarkAllAsRead` - Mark all notifications as read

### Applications
- `POST /Applications/CheckStatus` - Check application status
- `GET /Applications/DownloadLog` - Download application log
- `GET /Applications/GetExecutionHistory` - Get execution history

### Admin
- `POST /Admin/ToggleUserStatus` - Enable/disable user
- `POST /Admin/ToggleAdminStatus` - Grant/revoke admin rights
- `POST /Admin/DeleteFile` - Delete a file
- `POST /Admin/AddUserToRole` - Add user to role
- `POST /Admin/RemoveUserFromRole` - Remove user from role

## License

Copyright © 2025. All rights reserved.

## Support

For issues, questions, or contributions, please contact your system administrator.
