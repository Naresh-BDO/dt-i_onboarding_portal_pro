# Angular Application Update - Backend Integration Guide

## Overview
The Angular application has been updated to work with the new .NET backend signup flow that requires users to select a role during registration.

## Changes Made

### 1. **New Angular Models Created**

#### `signup-request.ts`
```typescript
export interface SignupRequest {
  username: string;
  email: string;
  password: string;
  roleId: number;
}
```

#### `signup-response.ts`
```typescript
export interface SignupResponse {
  userId: number;
  username: string;
  email: string;
  message: string;
}
```

#### `role-dto.ts`
```typescript
export interface RoleDto {
  id: number;
  name: string;
}
```

### 2. **Updated AuthService** (`auth.service.ts`)
Added three new methods:
- `signup(request: SignupRequest): Observable<SignupResponse>` - Handles user registration with role selection
- `getAvailableRoles(): Observable<RoleDto[]>` - Fetches available roles from backend
- Updated imports to include new models

### 3. **New Signup Component** (`signup/`)
Created complete signup component with:
- **signup.component.ts** - Component logic with:
  - Role loading on initialization
  - Form validation (password matching, minimum length)
  - Signup request handling
  - Automatic redirect to login on success
  - Error handling

- **signup.component.html** - User-friendly form with:
  - Username input
  - Email input
  - Password input
  - Password confirmation
  - Role selection dropdown (dynamically populated)
  - Submit button with loading state
  - Error and success messages
  - Link to login page

- **signup.component.css** - Modern styling with:
  - Gradient background
  - Responsive card layout
  - Form validation feedback
  - Smooth animations
  - Loading spinner

### 4. **Updated Login Component**
- Added link to signup page in footer
- Updated text from "Make sure API is running..." to "Don't have an account? Sign up here"

### 5. **Updated Routing** (`app-routing.module.ts`)
- Added signup route: `{ path: 'signup', component: SignupComponent }`
- Imported SignupComponent

### 6. **Updated Navbar** (`navbar.component.html`)
- Added "Sign Up" link for unauthenticated users
- Positioned next to "Login" link
- Link only visible when user is not authenticated

### 7. **.NET Backend DTOs** (Already in place)
The following DTOs exist in `DT_I_Onboarding_Portal.Core\Models\Dto`:
- `SignupRequest.cs` - Validates username, email, password, and roleId
- `SignupResponse.cs` - Returns registration confirmation
- `RoleDto.cs` - Represents available roles

## Backend API Endpoints

### `POST /api/auth/signup`
**Request:**
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123",
  "roleId": 1
}
```

**Response (201 Created):**
```json
{
  "userId": 1,
  "username": "john_doe",
  "email": "john@example.com",
  "message": "User registered successfully. You can now login."
}
```

### `GET /api/auth/roles`
**Response (200 OK):**
```json
[
  { "id": 1, "name": "Admin" },
  { "id": 2, "name": "User" },
  { "id": 3, "name": "Manager" }
]
```

## User Flow

1. **User visits `/signup`**
   - Component loads available roles from `/api/auth/roles`
   - Form displays with role dropdown

2. **User fills signup form**
   - Enters username, email, password, confirms password
   - Selects a role from dropdown
   - Validates:
     - All fields filled
     - Passwords match
     - Password minimum 6 characters
     - Valid email format

3. **User submits form**
   - POST request sent to `/api/auth/signup`
   - Backend validates and creates user with selected role
   - Success message shown
   - Auto-redirects to `/login` after 1.5 seconds

4. **User logs in**
   - Uses credentials from signup
   - Receives JWT token
   - Redirected based on role (Admin ? /new-joiner, Others ? /)

## Validation

### Frontend Validation
- Required fields check
- Password match validation
- Minimum password length (6 characters)
- Email format validation
- Role selection mandatory

### Backend Validation
- Username uniqueness (3-50 characters)
- Email format and uniqueness
- Password requirements (uppercase, lowercase, digit, special char, min 8)
- Role existence verification

## Error Handling

### Common Error Scenarios
| Error | Message |
|-------|---------|
| Username already exists | "Username already exists." |
| Email already registered | "Email already registered." |
| Invalid role | "Invalid role selected." |
| Server unreachable | "Cannot reach server. Check if https://localhost:7107 is running." |
| Password mismatch | "Passwords do not match." |
| Invalid email | "Valid email format required." |

## Files Modified/Created

### Angular Files
- ? `src/app/models/signup-request.ts` - **NEW**
- ? `src/app/models/signup-response.ts` - **NEW**
- ? `src/app/models/role-dto.ts` - **NEW**
- ? `src/app/signup/signup.component.ts` - **NEW**
- ? `src/app/signup/signup.component.html` - **NEW**
- ? `src/app/signup/signup.component.css` - **NEW**
- ? `src/app/auth/auth.service.ts` - **MODIFIED** (added signup & roles methods)
- ? `src/app/app-routing.module.ts` - **MODIFIED** (added signup route)
- ? `src/app/login/login.component.html` - **MODIFIED** (added signup link)
- ? `src/app/navbar/navbar.component.html` - **MODIFIED** (added signup link)

### .NET Files
- ? `DT-I_Onboarding_Portal.Core/Models/Dto/SignupRequest.cs` - **ALREADY EXISTS**
- ? `DT-I_Onboarding_Portal.Core/Models/Dto/SignupResponse.cs` - **ALREADY EXISTS**
- ? `DT-I_Onboarding_Portal.Core/Models/Dto/RoleDto.cs` - **ALREADY EXISTS**
- ? `DT-I_Onboarding_Portal.Server/Controllers/AuthController.cs` - **ALREADY UPDATED** (includes GetAvailableRoles & updated Signup)

## Testing Checklist

- [ ] Navigate to `/signup` and verify page loads
- [ ] Check that role dropdown populates with values
- [ ] Test form validation (empty fields, mismatched passwords)
- [ ] Submit valid signup form
- [ ] Verify success message appears
- [ ] Verify auto-redirect to login
- [ ] Test login with new account
- [ ] Verify role-based redirect works
- [ ] Test error scenarios (duplicate username/email)

## Security Considerations

? **Implemented:**
- Password hashing on backend (PasswordHasher)
- JWT token-based authentication
- Role validation before assignment
- Email format validation
- CORS configuration

?? **Recommendations:**
- Verify HTTPS is enabled in production
- Ensure JWT secret key is strong and stored securely
- Implement rate limiting on signup endpoint
- Add email verification before account activation
- Add password complexity requirements display on frontend

## Build Status
? **Build Successful** - All projects compile without errors

## Next Steps
1. Test the complete signup ? login ? redirect flow
2. Verify role-based access control
3. Set up seed data with initial roles in database
4. Deploy to production when ready
