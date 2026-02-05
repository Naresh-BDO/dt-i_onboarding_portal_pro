# Angular Application Architecture - Signup Feature

## Component Hierarchy

```
AppComponent
??? NavbarComponent
?   ??? [Auth Service] isAuthenticated(), getRoles(), logout()
?
??? LoginComponent
?   ??? [Auth Service]
?       ??? login()
?       ??? saveToken()
?
??? SignupComponent (NEW)
?   ??? [Auth Service]
?       ??? getAvailableRoles()
?       ??? signup()
?
??? HomeComponent
?   ??? [Auth Guard] - Public access
?
??? NewJoinerComponent
    ??? [Auth Guard + Role Guard] - Admin only
```

## Service Architecture

### AuthService (`auth.service.ts`)
**Location:** `src/app/auth/auth.service.ts`

**Dependencies:**
- HttpClient (Angular)
- BehaviorSubject (RxJS)

**Key Methods:**
```typescript
// Authentication
login(username, password): Observable<LoginResponse>
signup(request): Observable<SignupResponse>
saveToken(token): void
getToken(): string | null
logout(): void

// Role Management
getRoles(): string[]
isAdmin(): boolean

// State Management
isLoggedIn(): Observable<boolean>
isAuthenticated(): boolean

// New Methods
getAvailableRoles(): Observable<RoleDto[]>
```

**State Management:**
- Stores JWT token in localStorage
- Maintains login state via BehaviorSubject
- Auto-initializes from stored token on service creation

## Data Flow - Signup Process

```
[User Views /signup]
        ?
[SignupComponent - ngOnInit]
        ?
[AuthService.getAvailableRoles()]
        ?
[HTTP GET /api/auth/roles]
        ?
[Backend returns RoleDto[]]
        ?
[Populate role dropdown]
        ?
[User fills form & submits]
        ?
[Form Validation (Client-side)]
        ?? All fields filled?
        ?? Passwords match?
        ?? Password length >= 6?
        ?
[AuthService.signup(SignupRequest)]
        ?
[HTTP POST /api/auth/signup]
        ?
[Backend validates & creates user]
        ?
{Success}          {Error}
    ?                  ?
[Show message]    [Display error]
[Redirect login]  [Keep on form]
```

## Data Models

### Request Models
```typescript
// SignupRequest
{
  username: string;      // 3-50 chars
  email: string;         // Valid email
  password: string;      // Min 8, 1 upper, 1 lower, 1 digit, 1 special
  roleId: number;        // Must exist in database
}

// LoginRequest
{
  username: string;
  password: string;
}
```

### Response Models
```typescript
// SignupResponse
{
  userId: number;
  username: string;
  email: string;
  message: string;
}

// LoginResponse
{
  token: string;
  expires: DateTime;
  roles: string[];
}

// RoleDto
{
  id: number;
  name: string;
}
```

## Component Structure - SignupComponent

```
SignupComponent
?
??? Properties
?   ??? username: string
?   ??? email: string
?   ??? password: string
?   ??? confirmPassword: string
?   ??? selectedRoleId: number
?   ??? roles: RoleDto[]
?   ??? isSubmitting: boolean
?   ??? error: string
?   ??? success: string
?
??? Lifecycle
?   ??? ngOnInit() ? loadRoles()
?
??? Methods
?   ??? loadRoles(): void
?   ?   ??? Fetches available roles from service
?   ?
?   ??? onSignup(): void
?   ?   ??? Validates form (client-side)
?   ?   ??? Creates SignupRequest
?   ?   ??? Calls authService.signup()
?   ?   ??? Handles success (redirect)
?   ?   ??? Handles errors
?   ?
?   ??? Template Binding (Two-way)
?       ??? [(ngModel)]="username"
?       ??? [(ngModel)]="email"
?       ??? [(ngModel)]="password"
?       ??? [(ngModel)]="confirmPassword"
?       ??? [(ngModel)]="selectedRoleId"
?
??? Conditional Rendering
    ??? *ngIf="!isSubmitting" ? Show button text
    ??? *ngIf="isSubmitting" ? Show spinner
    ??? *ngFor="let role of roles" ? Populate dropdown
    ??? *ngIf="error" ? Show error alert
    ??? *ngIf="success" ? Show success alert
```

## Service Dependency Injection

```typescript
// AuthService injection in SignupComponent
constructor(
  private auth: AuthService,    // Core authentication logic
  private router: Router         // Navigation after signup
) { }

// AuthService injection in AppComponent/various components
constructor(private auth: AuthService) { }
```

## HTTP Request/Response Flow

### Signup Request
```
POST /api/auth/signup HTTP/1.1
Host: localhost:7107
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "roleId": 2
}
```

### Signup Response (Success)
```
HTTP/1.1 201 Created
Content-Type: application/json

{
  "userId": 5,
  "username": "john_doe",
  "email": "john@example.com",
  "message": "User registered successfully. You can now login."
}
```

### Signup Response (Error)
```
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "message": "Username already exists."
}
```

## Error Handling Flow

```
HTTP Error
    ?
[Catch in subscribe()]
    ?
    ?? [401/400] ? Parse error.error.message
    ?? [0] ? Network error
    ?? [other] ? Generic error
    ?
[Set this.error = message]
    ?
[Display in template alert]
    ?
[Set isSubmitting = false]
    ?
[User can retry]
```

## State Management

### Component State
- Local to SignupComponent
- Managed via class properties
- Bound to template via two-way binding

### Application State
- JWT token stored in localStorage
- Accessed via AuthService.getToken()
- Auth state managed via BehaviorSubject in AuthService
- Persists across page refreshes

### Reactive State
- Uses RxJS Observables
- Auto-unsubscribe not needed (HTTP Observables complete)
- Could implement OnDestroy for manual cleanup if needed

## Routing Configuration

```typescript
const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'login', component: LoginComponent },
  { path: 'signup', component: SignupComponent },         // NEW
  { path: 'new-joiner', component: NewJoinerComponent },  // Protected
];
```

## Module Dependencies

### SignupComponent Imports
```typescript
imports: [
  CommonModule,  // *ngIf, *ngFor, etc.
  FormsModule    // [(ngModel)] two-way binding
]
```

### AuthService Dependencies
```typescript
constructor(private http: HttpClient)
```

## File Organization

```
src/app/
??? auth/
?   ??? auth.service.ts         ? Core auth logic
?   ??? auth.guard.ts           ? Route protection
?   ??? role.guard.ts           ? Role-based access
?
??? models/                      ? Data transfer objects
?   ??? login-request.ts
?   ??? login-response.ts
?   ??? signup-request.ts        ? NEW
?   ??? signup-response.ts       ? NEW
?   ??? role-dto.ts              ? NEW
?
??? signup/                      ? NEW Signup feature
?   ??? signup.component.ts
?   ??? signup.component.html
?   ??? signup.component.css
?
??? login/
?   ??? login.component.ts
?   ??? login.component.html
?   ??? login.component.css
?
??? navbar/
?   ??? navbar.component.html    ? MODIFIED (signup link)
?
??? home/
??? new-joiner/
?
??? app-routing.module.ts        ? MODIFIED (signup route)
```

## Performance Considerations

### Optimization Opportunities
1. **Lazy Loading:** Load SignupComponent only on demand
2. **Role Caching:** Cache roles list (10min TTL)
3. **Debouncing:** Debounce username/email uniqueness checks
4. **Change Detection:** Use OnPush strategy for better performance

### Current Implementation
- Roles fetched fresh each time signup page loads
- Suitable for small number of roles
- Consider caching if 100+ roles exist

## Security Considerations

### Frontend Security
? Password validation before submit
? HTTPS communication enforced
? Token stored securely in localStorage
? Auto-logout on token expiry (can be added)

### Backend Validation
? Server-side password validation
? Username/email uniqueness check
? Role existence verification
? JWT token validation

### Recommendations
?? Implement email verification before account activation
?? Add rate limiting on signup endpoint
?? Implement CAPTCHA for bot protection
?? Add password complexity display on frontend
