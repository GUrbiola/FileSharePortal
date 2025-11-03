# Role-Based File Sharing Fix

## Issue
When files were shared using roles, the application was not properly detecting that users who are members of those roles (including through distribution lists) had access to the files. This affected both the web UI and the API.

## Root Cause
The access checking logic in both `FilesController.cs` (MVC) and `FilesApiController.cs` (API) was only checking for **direct user shares** and not expanding role memberships to include distribution list members.

### Example of Problematic Code:
```csharp
// OLD CODE - Only checked direct user shares
bool hasAccess = file.UploadedByUserId == currentUser.UserId ||
               currentUser.IsAdmin ||
               _context.FileShares.Any(fs => fs.FileId == id &&
                   fs.SharedWithUserId == currentUser.UserId);
```

This query only looked at `SharedWithUserId` and ignored `SharedWithRoleId`.

## Solution

### 1. Created Helper Method: `HasUserAccessToFile`

Added a comprehensive helper method to both controllers that:
- Checks if user is the file owner
- Checks if user is an admin
- Checks if file is shared directly with the user
- **Checks if file is shared with any role the user belongs to (including through distribution lists)**
- Optionally checks download permissions

```csharp
private bool HasUserAccessToFile(int fileId, int userId, bool requireDownloadPermission = false)
{
    var file = _context.SharedFiles.Find(fileId);
    if (file == null)
        return false;

    // Owner and admin always have access
    var user = _context.Users.Find(userId);
    if (file.UploadedByUserId == userId || (user != null && user.IsAdmin))
        return true;

    // Check direct user shares
    var directShare = _context.FileShares.FirstOrDefault(fs =>
        fs.FileId == fileId && fs.SharedWithUserId == userId);

    if (directShare != null)
    {
        if (requireDownloadPermission)
            return directShare.CanDownload;
        return true;
    }

    // Check role shares (including distribution list members)
    var roleShares = _context.FileShares
        .Where(fs => fs.FileId == fileId && fs.SharedWithRoleId.HasValue)
        .ToList();

    foreach (var roleShare in roleShares)
    {
        // This expands roles to include all members, including from distribution lists
        var roleUsers = _roleService.GetRoleUsers(roleShare.SharedWithRoleId.Value);
        if (roleUsers.Any(u => u.UserId == userId))
        {
            if (requireDownloadPermission)
                return roleShare.CanDownload;
            return true;
        }
    }

    return false;
}
```

### 2. Updated Access Checks

#### FilesController.cs (MVC)
Updated the following methods:
- **Details** (Line ~330): Check access before showing file details
- **Preview** (Line ~369): Check access before showing file preview
- **Download** (Line ~394): Check access with download permission requirement
- **SharedWithMe** (Line ~41): Completely rewrote to expand role memberships

#### FilesApiController.cs (API)
Updated the following methods:
- **Download** (Line ~178): Check access with download permission requirement

## How It Works Now

### Role Membership Expansion
The `RoleService.GetRoleUsers()` method correctly expands role memberships:

1. **Gets direct role members** from `RoleUsers` table
2. **Gets distribution lists** associated with the role from `RoleDistributionLists` table
3. **Queries Active Directory** for each distribution list to get all members
4. **Auto-creates users** from AD if they don't exist in the database
5. **Combines and deduplicates** all users

### Access Flow

When a user tries to access a file:

1. Check if user is the file owner → **Grant Access**
2. Check if user is an admin → **Grant Access**
3. Check if file is shared directly with user → **Grant Access** (respecting download permission if needed)
4. Get all role-based shares for the file
5. For each role share:
   - Expand the role to get all members (including from distribution lists)
   - Check if current user is in that list → **Grant Access** (respecting download permission if needed)
6. If none of the above → **Deny Access**

## Files Modified

### Controllers/FilesController.cs
- Added `HasUserAccessToFile()` method (Lines 192-235)
- Updated `SharedWithMe()` action (Lines 41-76)
- Updated `Details()` action (Lines 318-333)
- Updated `Preview()` action (Lines 358-372)
- Updated `Download()` action (Lines 383-397)

### Controllers/Api/FilesApiController.cs
- Added `HasUserAccessToFile()` method (Lines 418-461)
- Updated `Download()` action (Lines 177-181)

## Testing Checklist

To verify the fix works:

1. **Setup:**
   - Create a role (e.g., "Marketing")
   - Add a distribution list to the role (e.g., "DL-Marketing")
   - Ensure there are users in that distribution list

2. **Test File Sharing:**
   - Upload a file as User A
   - Share the file with the "Marketing" role (not directly with users)
   - Log in as User B (member of DL-Marketing)
   - User B should see the file in "Shared With Me"
   - User B should be able to view file details
   - User B should be able to preview the file (if applicable)
   - User B should be able to download the file

3. **Test API:**
   - Authenticate as User B via API
   - GET `/api/files/shared` should include the file
   - GET `/api/files/{id}/download` should allow download

4. **Test Notifications:**
   - When file is shared with role, all role members (including DL members) should receive notifications
   - When role share is removed, all role members should receive removal notifications

## Benefits

✅ **Distribution list members** now have proper access to files shared with their roles
✅ **Consistent behavior** between web UI and API
✅ **Automatic user creation** from AD when they access shared files
✅ **Proper permission enforcement** (view vs. download)
✅ **Performance optimized** by reusing `RoleService.GetRoleUsers()`
✅ **Notifications work correctly** for role-based shares

## Notes

- The existing `GetUsersWithAccess()` method was already correct and is used for calculating notifications
- The `RoleService.GetRoleUsers()` method correctly handles distribution lists and caches results
- Download permission is still respected when checking access through roles
- Admin users always have full access regardless of shares
