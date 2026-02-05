# Quick Start Guide - Signup Feature

## Prerequisites
- .NET 8 backend running on `https://localhost:7107`
- Angular development server running
- Database with at least one role defined

## Setup Database Roles (if not already done)

Run this SQL script on your `Bdo` database:

```sql
INSERT INTO Roles (Name) VALUES ('Admin');
INSERT INTO Roles (Name) VALUES ('User');
INSERT INTO Roles (Name) VALUES ('Manager');
```

Or use .NET CLI:
```bash
# In the solution directory
dotnet ef database update --project DT-I_Onboarding_Portal.Data
```

## Running the Application

### Backend
```bash
cd DT-I_Onboarding_Portal.Server
dotnet run
```
API will be available at: `https://localhost:7107`

### Frontend
```bash
cd dt-i_onboarding_portal.client
npm install  # Only if dependencies changed
npm start
```
Frontend will be available at: `https://localhost:4200`

## Testing the Signup Flow

### Step 1: Navigate to Signup
```
https://localhost:4200/signup
```

### Step 2: Fill Form
- **Username:** testuser
- **Email:** testuser@example.com
- **Password:** Password123!
- **Confirm Password:** Password123!
- **Role:** Select "User"

### Step 3: Submit
Click "Create Account" button

### Step 4: Expected Results
? Success message: "Account created successfully! Redirecting to login..."
? Auto-redirect to login page after ~1.5 seconds

### Step 5: Login with New Account
- **Username:** testuser
- **Password:** Password123!

Click "Login"

### Step 6: Expected Redirect
- If role is "Admin" ? Redirected to `/new-joiner`
- Otherwise ? Redirected to `/` (home)

## Troubleshooting

### Issue: "Cannot reach server"
**Solution:** Ensure backend is running on `https://localhost:7107`
```bash
dotnet run
```

### Issue: "No roles available"
**Solution:** Add roles to database (see Setup section above)

### Issue: "Username already exists"
**Solution:** Use a different username or delete the test user from database

### Issue: CORS error in console
**Solution:** Check backend CORS configuration in `Program.cs`
- Verify `AllowFrontend` policy includes `localhost:4200`

### Issue: Password validation fails
**Solution:** Backend requires:
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character (@$!%*?&)

Example valid password: `SecurePass123!`

## API Endpoints Reference

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/auth/roles` | Fetch available roles |
| POST | `/api/auth/signup` | Create new user account |
| POST | `/api/auth/login` | Authenticate user |
| GET | `/api/auth/whoami` | Get current user info |
| GET | `/api/auth/profile/{id}` | Get user profile |

## Common Validation Errors

| Error Message | Cause | Fix |
|---------------|-------|-----|
| "Username already exists" | Username taken | Choose different username |
| "Email already registered" | Email taken | Use different email |
| "Invalid role selected" | Role doesn't exist | Select valid role from dropdown |
| "Passwords do not match" | Password mismatch | Ensure confirm password matches |
| "Please fill in all fields" | Missing field | Complete all form fields |

## Frontend Routes

| Route | Component | Access |
|-------|-----------|--------|
| `/` | Home | Public |
| `/login` | Login | Public |
| `/signup` | Signup | Public |
| `/new-joiner` | New Joiner | Admin only |

## Database Schema

### Users Table
```
Id (PK), Username (unique), Email, PasswordHash, IsActive
```

### Roles Table
```
Id (PK), Name
```

### UserRoles Table
```
UserId (FK), RoleId (FK) - Composite PK
```

## Performance Tips

- Roles are fetched once on signup page load
- Validation happens client-side first, then server-side
- Token is stored in localStorage for persistence
- Consider implementing role caching if you have many roles

## Security Reminders

?? **Before Production:**
1. Change default JWT secret key
2. Enable HTTPS everywhere
3. Implement email verification
4. Add rate limiting to signup endpoint
5. Enable HTTPS metadata requirement in JWT config
6. Use environment variables for sensitive config

## Support

For issues:
1. Check browser console for errors (F12)
2. Check backend logs in terminal
3. Verify database connection
4. Ensure all dependencies are installed
