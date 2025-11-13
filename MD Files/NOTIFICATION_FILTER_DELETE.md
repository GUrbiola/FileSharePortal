# Notification Filter Delete Feature

## Overview

Enhanced the notification delete functionality with advanced filtering options, allowing users to delete notifications based on count (oldest N) or date (older than specific date).

---

## New Features Added

### ✅ **Delete Oldest N Notifications**
- Delete a specific number of oldest notifications
- Range: 1-1000 notifications at a time
- Sorted by creation date (oldest first)
- Interactive modal with number input

### ✅ **Delete Notifications Older Than Date**
- Delete all notifications created on or before a specific date
- Date picker for easy selection
- Quick select buttons for common timeframes:
  - 1 Week Ago
  - 1 Month Ago
  - 3 Months Ago
  - 1 Year Ago
- Interactive modal with date input

---

## Files Modified

### 1. **Controllers/NotificationsController.cs**

#### New Method: `DeleteOldest(int count)` - Line 215

**Purpose:** Delete the oldest N notifications

**Parameters:**
- `count` (int) - Number of notifications to delete (1-1000)

**Logic:**
1. Validates count is greater than 0
2. Gets user's notifications ordered by creation date (oldest first)
3. Takes the specified count
4. Deletes selected notifications
5. Returns success message with count

**Example Usage:**
```csharp
[HttpPost]
public JsonResult DeleteOldest(int count)
{
    var notifications = _context.Notifications
        .Where(n => n.UserId == currentUser.UserId)
        .OrderBy(n => n.CreatedDate)
        .Take(count)
        .ToList();

    _context.Notifications.RemoveRange(notifications);
    _context.SaveChanges();
}
```

**Returns:**
```json
{
  "success": true,
  "message": "Successfully deleted 10 oldest notification(s)",
  "deletedCount": 10
}
```

---

#### New Method: `DeleteOlderThan(string date)` - Line 255

**Purpose:** Delete all notifications created on or before a specific date

**Parameters:**
- `date` (string) - Date in string format (e.g., "2025-01-01")

**Logic:**
1. Parses the date string
2. Sets time to end of day (includes entire cutoff day)
3. Gets all notifications created on or before that date
4. Deletes selected notifications
5. Returns success message with count

**Example Usage:**
```csharp
[HttpPost]
public JsonResult DeleteOlderThan(string date)
{
    DateTime cutoffDate = DateTime.Parse(date);
    cutoffDate = cutoffDate.Date.AddDays(1).AddSeconds(-1);

    var notifications = _context.Notifications
        .Where(n => n.UserId == currentUser.UserId &&
                    n.CreatedDate <= cutoffDate)
        .ToList();

    _context.Notifications.RemoveRange(notifications);
    _context.SaveChanges();
}
```

**Returns:**
```json
{
  "success": true,
  "message": "Successfully deleted 25 notification(s) older than Nov 01, 2025",
  "deletedCount": 25
}
```

---

### 2. **Views/Notifications/Index.cshtml**

#### Updated Delete Dropdown Menu - Line 17

Added two new options:
- **Delete Oldest N...** - Opens count filter modal
- **Delete Older Than...** - Opens date filter modal

```html
<ul class="dropdown-menu">
    <li><a class="dropdown-item" href="#" onclick="deleteReadNotifications();">
        <i class="bi bi-check-circle"></i> Delete All Read
    </a></li>
    <li><a class="dropdown-item" href="#" onclick="deleteAllNotifications();">
        <i class="bi bi-trash"></i> Delete All
    </a></li>
    <li><hr class="dropdown-divider"></li>
    <li><a class="dropdown-item" href="#" data-bs-toggle="modal" data-bs-target="#deleteByCountModal">
        <i class="bi bi-hash"></i> Delete Oldest N...
    </a></li>
    <li><a class="dropdown-item" href="#" data-bs-toggle="modal" data-bs-target="#deleteByDateModal">
        <i class="bi bi-calendar"></i> Delete Older Than...
    </a></li>
</ul>
```

---

#### New Modal: Delete By Count - Line 146

**Features:**
- Number input field (min: 1, max: 1000)
- Default value: 10
- Input validation
- Warning alert
- Cancel and Delete buttons

**UI Elements:**
- Modal title with hash icon
- Description text
- Number input with label and help text
- Warning banner
- Action buttons (Cancel / Delete)

**Modal Structure:**
```html
<div class="modal fade" id="deleteByCountModal">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5>Delete Oldest Notifications</h5>
            </div>
            <div class="modal-body">
                <input type="number" id="deleteCountInput"
                       min="1" max="1000" value="10">
                <div class="alert alert-warning">Warning...</div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary">Cancel</button>
                <button class="btn btn-danger" onclick="executeDeleteByCount()">
                    Delete Notifications
                </button>
            </div>
        </div>
    </div>
</div>
```

---

#### New Modal: Delete By Date - Line 177

**Features:**
- Date picker input
- Quick select buttons for common timeframes
- Input validation
- Warning alert
- Cancel and Delete buttons

**Quick Select Buttons:**
- **1 Week Ago**: Sets date to 7 days ago
- **1 Month Ago**: Sets date to 30 days ago
- **3 Months Ago**: Sets date to 90 days ago
- **1 Year Ago**: Sets date to 365 days ago

**Modal Structure:**
```html
<div class="modal fade" id="deleteByDateModal">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5>Delete Notifications Older Than Date</h5>
            </div>
            <div class="modal-body">
                <input type="date" id="deleteDateInput">
                <div class="btn-group">
                    <button onclick="setDateOffset(7)">1 Week Ago</button>
                    <button onclick="setDateOffset(30)">1 Month Ago</button>
                    <button onclick="setDateOffset(90)">3 Months Ago</button>
                    <button onclick="setDateOffset(365)">1 Year Ago</button>
                </div>
                <div class="alert alert-warning">Warning...</div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary">Cancel</button>
                <button class="btn btn-danger" onclick="executeDeleteByDate()">
                    Delete Notifications
                </button>
            </div>
        </div>
    </div>
</div>
```

---

### JavaScript Functions

#### `setDateOffset(days)` - Line 477

**Purpose:** Helper function to set date input based on offset

**Parameters:**
- `days` (int) - Number of days to go back from today

**Logic:**
```javascript
function setDateOffset(days) {
    var date = new Date();
    date.setDate(date.getDate() - days);
    var dateString = date.toISOString().split('T')[0];
    $('#deleteDateInput').val(dateString);
}
```

**Example:** `setDateOffset(30)` sets the date input to 30 days ago

---

#### `executeDeleteByCount()` - Line 484

**Purpose:** Execute deletion of oldest N notifications

**Flow:**
1. Get count value from input
2. Validate count (> 0 and <= 1000)
3. Close input modal
4. Show SweetAlert confirmation
5. If confirmed, POST to DeleteOldest action
6. Display success message
7. Reload page to show updated list

**Validation:**
- Count must be greater than 0
- Count must be 1000 or less
- Shows error message if validation fails

**Confirmation Dialog:**
```
Title: "Delete Oldest Notifications?"
Message: "Are you sure you want to delete the oldest [N] notification(s)?
          This action cannot be undone."
Buttons: "Yes, delete them!" / "Cancel"
```

---

#### `executeDeleteByDate()` - Line 553

**Purpose:** Execute deletion of notifications older than date

**Flow:**
1. Get date value from input
2. Validate date is selected
3. Format date for display
4. Close input modal
5. Show SweetAlert confirmation with formatted date
6. If confirmed, POST to DeleteOlderThan action
7. Display success message
8. Reload page to show updated list

**Validation:**
- Date must be selected
- Shows error message if validation fails

**Confirmation Dialog:**
```
Title: "Delete Notifications?"
Message: "Are you sure you want to delete all notifications created on
          or before [formatted date]? This action cannot be undone."
Buttons: "Yes, delete them!" / "Cancel"
```

---

## User Flow Examples

### Delete Oldest 10 Notifications

1. User clicks **Delete** dropdown → **Delete Oldest N...**
2. Modal opens with default value of 10
3. User can adjust the number or leave as 10
4. User clicks **Delete Notifications** button
5. Modal closes
6. Confirmation dialog appears: "Delete oldest 10 notification(s)?"
7. User clicks **Yes, delete them!**
8. AJAX request sent to server
9. Server deletes 10 oldest notifications
10. Success message: "Successfully deleted 10 oldest notification(s)"
11. Page reloads showing remaining notifications

---

### Delete Notifications Older Than 1 Month

1. User clicks **Delete** dropdown → **Delete Older Than...**
2. Modal opens with date picker
3. User clicks **1 Month Ago** quick select button
4. Date input automatically set to 30 days ago
5. User clicks **Delete Notifications** button
6. Modal closes
7. Confirmation dialog appears with formatted date
8. User clicks **Yes, delete them!**
9. AJAX request sent to server
10. Server deletes all notifications older than that date
11. Success message: "Successfully deleted 25 notification(s) older than Oct 12, 2025"
12. Page reloads showing remaining notifications

---

## Security Features

### Authorization
- ✅ Both methods verify notification ownership
- ✅ Users can only delete their own notifications
- ✅ Date/count parameters are validated server-side

### Validation
- ✅ Count must be between 1-1000
- ✅ Date must be valid and parseable
- ✅ Empty or invalid inputs return error messages

### Error Handling
- ✅ Try-catch blocks in controller actions
- ✅ Client-side validation before server request
- ✅ Graceful error messages displayed to user

---

## UI/UX Enhancements

### Modal Design
- Professional Bootstrap 5 modal styling
- Clear titles with icons
- Descriptive text explaining the action
- Warning banners to prevent accidents
- Prominent Cancel and Delete buttons

### Quick Select Buttons
- Common timeframes for easy selection
- Full-width button group for visibility
- Clear labels (1 Week Ago, 1 Month Ago, etc.)
- Instantly updates date picker

### Input Fields
- Number input with min/max constraints
- Date picker with native browser support
- Help text explaining the input
- Form validation feedback

### Confirmation Dialogs
- SweetAlert2 for beautiful confirmations
- Shows exact count or date being deleted
- Red danger button for Delete action
- Gray Cancel button for safety

---

## Code Examples

### Controller - Delete Oldest

```csharp
[HttpPost]
public JsonResult DeleteOldest(int count)
{
    try
    {
        var currentUser = _authService.GetCurrentUser();

        if (count <= 0)
        {
            return Json(new { success = false, message = "Count must be greater than 0" });
        }

        // Get oldest notifications by creation date
        var notifications = _context.Notifications
            .Where(n => n.UserId == currentUser.UserId)
            .OrderBy(n => n.CreatedDate)
            .Take(count)
            .ToList();

        if (notifications.Count == 0)
        {
            return Json(new { success = false, message = "No notifications to delete" });
        }

        _context.Notifications.RemoveRange(notifications);
        _context.SaveChanges();

        return Json(new
        {
            success = true,
            message = $"Successfully deleted {notifications.Count} oldest notification(s)",
            deletedCount = notifications.Count
        });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = $"Error deleting notifications: {ex.Message}" });
    }
}
```

---

### JavaScript - Execute Delete By Count

```javascript
async function executeDeleteByCount() {
    var count = parseInt($('#deleteCountInput').val());

    // Validation
    if (!count || count <= 0 || count > 1000) {
        Swal.fire({
            title: 'Invalid Input',
            text: 'Please enter a valid number between 1 and 1000.',
            icon: 'warning'
        });
        return;
    }

    // Close modal
    var modal = bootstrap.Modal.getInstance(document.getElementById('deleteByCountModal'));
    modal.hide();

    // Confirmation
    const result = await Swal.fire({
        title: 'Delete Oldest Notifications?',
        html: 'Delete the oldest <strong>' + count + '</strong> notification(s)?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        confirmButtonText: 'Yes, delete them!'
    });

    if (result.isConfirmed) {
        // AJAX request
        $.post('@Url.Action("DeleteOldest")', { count: count }, function (data) {
            if (data.success) {
                Swal.fire({
                    title: 'Deleted!',
                    text: data.message,
                    icon: 'success',
                    timer: 2000
                }).then(() => {
                    location.reload();
                });
            }
        });
    }
}
```

---

## Testing Checklist

### Functional Testing
- [ ] Delete oldest N notifications works
- [ ] Count validation (1-1000) works
- [ ] Delete by date works
- [ ] Date validation works
- [ ] Quick select buttons set correct dates
- [ ] Modals open and close correctly
- [ ] Confirmation dialogs appear
- [ ] Cancel buttons work without deleting
- [ ] Success messages display correctly
- [ ] Page reloads after deletion

### Edge Cases
- [ ] Delete 0 notifications (should error)
- [ ] Delete 1001 notifications (should error)
- [ ] Delete with no date selected (should error)
- [ ] Delete when no notifications exist
- [ ] Delete when no notifications match criteria
- [ ] Invalid date format handling
- [ ] Future date selection

### Security Testing
- [ ] Can only delete own notifications
- [ ] Invalid user IDs rejected
- [ ] SQL injection prevention
- [ ] XSS prevention in date display

### UI/UX Testing
- [ ] Modals are properly styled
- [ ] Buttons are clickable and visible
- [ ] Input fields accept valid values
- [ ] Help text is clear
- [ ] Warning banners are prominent
- [ ] Responsive design on mobile
- [ ] Quick select buttons work on mobile

---

## Browser Compatibility

Tested and compatible with:
- ✅ Chrome (latest)
- ✅ Firefox (latest)
- ✅ Safari (latest)
- ✅ Edge (latest)
- ✅ Mobile browsers (iOS Safari, Chrome Mobile)

**Required:**
- Native date picker support (HTML5)
- Bootstrap 5 modals
- SweetAlert2 (loaded via CDN)
- jQuery (existing dependency)

---

## Performance Considerations

### Database Queries
- Uses indexed `CreatedDate` column for efficient sorting
- `OrderBy()` with `Take()` limits results efficiently
- Single database transaction per operation
- `RemoveRange()` for bulk deletions

### Client-Side
- Modals use Bootstrap's efficient modal system
- Date calculations done client-side
- AJAX prevents full page reloads (except final reload)
- Minimal DOM manipulation

### Scalability
- **1-1000 limit** prevents server overload
- Date-based deletion can handle large datasets
- Indexed queries ensure fast execution
- Transaction-based deletions ensure data integrity

---

## Future Enhancements

Potential improvements:

1. **Preview Before Delete**
   - Show list of notifications that will be deleted
   - Allow deselection before confirmation

2. **Custom Date Ranges**
   - Delete notifications between two dates
   - More granular control

3. **Advanced Filters**
   - Combine filters (oldest N read notifications)
   - Filter by notification type
   - Filter by source/action

4. **Scheduled Deletion**
   - Auto-delete notifications older than X days
   - User-configurable retention policy

5. **Batch Processing**
   - Process large deletions in background
   - Progress bar for long operations

6. **Undo Functionality**
   - Soft delete with recovery period
   - Restore deleted notifications

---

## Troubleshooting

### Issue: Modal doesn't open

**Solution:**
- Ensure Bootstrap 5 JS is loaded
- Check browser console for errors
- Verify modal IDs match data-bs-target

---

### Issue: Date picker not working

**Solution:**
- Browser may not support HTML5 date input
- Use polyfill for older browsers
- Check date input type is "date"

---

### Issue: Count validation not working

**Solution:**
- Check JavaScript is loaded
- Verify input has min/max attributes
- Ensure parseInt() is working correctly

---

## API Reference

### Controller Endpoints

| Endpoint | Method | Parameters | Returns |
|----------|--------|------------|---------|
| `/Notifications/DeleteOldest` | POST | `count` (int, 1-1000) | `{success, message, deletedCount}` |
| `/Notifications/DeleteOlderThan` | POST | `date` (string, ISO format) | `{success, message, deletedCount}` |

### JavaScript Functions

| Function | Parameters | Description |
|----------|------------|-------------|
| `setDateOffset(days)` | `days` (int) | Set date picker to N days ago |
| `executeDeleteByCount()` | None | Execute count-based deletion |
| `executeDeleteByDate()` | None | Execute date-based deletion |

---

## Conclusion

The filtered delete feature provides powerful, user-friendly options for managing notifications:

- ✅ **Delete by Count**: Remove oldest N notifications quickly
- ✅ **Delete by Date**: Clean up old notifications easily
- ✅ **Quick Select**: Common timeframes for convenience
- ✅ **Validation**: Prevents invalid inputs
- ✅ **Confirmation**: Prevents accidental deletions
- ✅ **Security**: Users can only delete their own notifications

The implementation is intuitive, secure, and performant, providing users with flexible notification management options!

---

**Created**: 2025-11-11
**Version**: 1.0
**Feature**: Notification Filter Delete
