# GitHub OAuth Authentication - Setup Instructions

## Overview

This document provides instructions for setting up GitHub OAuth authentication for GitBuddy.

## Prerequisites

- A GitHub account
- Administrator access to the GitBuddy application

## Step 1: Create GitHub OAuth App

1. Go to GitHub > Settings > Developer settings > OAuth Apps
2. Click "New OAuth App"
3. Fill in the form:
   - **Application name**: GitBuddy
   - **Homepage URL**: `http://localhost:5173`
   - **Application description**: (optional) GitHub PR Dashboard
   - **Authorization callback URL**: `http://localhost:5247/api/auth/github/callback`
4. Click "Register application"
5. Copy the **Client ID** and generate a new **Client Secret**
6. **IMPORTANT**: Keep these values secure!

## Step 2: Configure Backend

1. Open `GitBuddy.Api/appsettings.Development.json`
2. Update the GitHub configuration:
   ```json
   {
     "GitHub": {
       "ClientId": "YOUR_GITHUB_CLIENT_ID_HERE",
       "ClientSecret": "YOUR_GITHUB_CLIENT_SECRET_HERE",
       "RedirectUri": "http://localhost:5247/api/auth/github/callback"
     }
   }
   ```

3. For production, update `appsettings.json` with:
   - Production GitHub Client ID and Secret
   - Secure JWT key (change the default!)
   - Update redirect URLs to production domain
   - Update Frontend URL to production domain

## Step 3: Run Database Migration

The migration has already been created. Run it to update the database schema:

```bash
cd GitBuddy.Api
dotnet ef database update
```

This will add the following columns to the Users table:
- `Provider` - OAuth provider (e.g., "GitHub")
- `ProviderUserId` - GitHub user ID
- `AvatarUrl` - GitHub profile picture URL
- `LastLoginAt` - Last login timestamp
- `AccessToken` - GitHub OAuth token

## Step 4: Start the Applications

### Backend

```bash
cd GitBuddy.Api
dotnet run
```

The API will be available at `http://localhost:5247`

### Frontend

```bash
cd gitbuddy-vue
npm install  # If not already installed
npm run dev
```

The frontend will be available at `http://localhost:5173`

## Step 5: Test the Authentication Flow

1. Open `http://localhost:5173` in your browser
2. You should see an "Authentication Required" message with a "Login with GitHub" button
3. Click "Login with GitHub" button
4. You'll be redirected to GitHub to authorize the app
5. After authorization, you'll be redirected back to GitBuddy
6. You should now see the pull request dashboard with your GitHub avatar and username in the header
7. The JWT token is automatically stored in localStorage and sent with API requests
8. If your token expires (7 days), you'll be automatically logged out and prompted to login again

## Authentication Features Implemented

### Backend
- ✅ GitHub OAuth 2.0 authentication flow
- ✅ JWT token generation and validation
- ✅ User creation/updating from GitHub profile
- ✅ Protected API endpoints with `[Authorize]` attribute
- ✅ Authentication middleware configuration
- ✅ Database schema for OAuth users

### Frontend
- ✅ Pinia store for auth state management
- ✅ JWT token injection via axios interceptors
- ✅ Login button component
- ✅ User menu component with logout
- ✅ OAuth callback handling
- ✅ Route guards for protected routes
- ✅ Auto-logout on 401 errors

### Protected Endpoints

**All API endpoints now require authentication** except for:

- `/api/auth/github` (GET) - OAuth login initiation
- `/api/auth/github/callback` (GET) - OAuth callback

This means the entire application is protected and no data is accessible to unauthenticated users.

## JWT Configuration

Default JWT settings (in `appsettings.json`):

```json
{
  "Jwt": {
    "Key": "your-super-secret-key-at-least-32-characters-long-change-this-in-production",
    "Issuer": "GitBuddy",
    "Audience": "GitBuddyUsers",
    "ExpirationMinutes": 10080
  }
}
```

**⚠️ SECURITY WARNING**: Change the JWT key in production! Use a strong, randomly generated secret and store it securely (e.g., environment variables, Azure Key Vault, etc.).

Token expiration is set to 7 days (10080 minutes). Adjust as needed.

## Troubleshooting

### "Authentication Required" message on homepage

- The application now requires authentication for all endpoints
- You must log in with GitHub to access any data
- Click "Login with GitHub" button to authenticate

### "401 Unauthorized" errors

- Check that you're logged in (click Login with GitHub)
- Check that your JWT token hasn't expired (default: 7 days)
- Clear localStorage and try logging in again
- On 401 error, the page will automatically reload and show login prompt

### OAuth callback fails

- Verify GitHub OAuth App configuration
- Check that redirect URI matches exactly: `http://localhost:5247/api/auth/github/callback`
- Verify Client ID and Secret are correct in appsettings.json
- Check browser console for errors

### Database migration fails

- Make sure you're in the `GitBuddy.Api` directory
- Ensure SQLite database file isn't locked by another process
- Try running `dotnet ef migrations remove` and `dotnet ef migrations add AddOAuthFieldsToUser` again

### CORS errors

- Check that CORS configuration in `Program.cs` includes your frontend URL
- Ensure frontend is running on `http://localhost:5173`

## Production Deployment Considerations

1. **HTTPS**: Enable HTTPS for both frontend and backend
2. **Environment Variables**: Store sensitive values (GitHub secrets, JWT key) in environment variables
3. **GitHub OAuth URLs**: Update to production domain
4. **JWT Key**: Use a strong, randomly generated secret
5. **Rate Limiting**: Consider adding rate limiting to auth endpoints
6. **Refresh Tokens**: For better UX, implement refresh tokens instead of long-lived JWTs
7. **Logout**: Clear token on logout (already implemented on client-side)

## File Changes Summary

### Backend (C#)
- `GitBuddy.Api.csproj` - Added JWT and OAuth packages
- `GitBuddy.Domain/Models/User.cs` - Added OAuth fields
- `DTOs/AuthDtos.cs` - NEW - Authentication DTOs
- `Services/JwtService.cs` - NEW - JWT token handling
- `Services/UserService.cs` - GitHub user handling
- `Controllers/AuthController.cs` - NEW - OAuth endpoints
- `Controllers/UserPreferencesController.cs` - Added [Authorize]
- `Controllers/SettingsController.cs` - Added [Authorize]
- `Controllers/PullRequestsController.cs` - Added [Authorize] to POST endpoints
- `Program.cs` - Added authentication middleware
- `appsettings.json` - Added GitHub and JWT config
- `appsettings.Development.json` - Added dev config
- `Migrations/AddOAuthFieldsToUser.cs` - NEW - Database migration

### Frontend (Vue)
- `package.json` - Added Pinia
- `src/main.ts` - Added Pinia
- `src/stores/auth.ts` - NEW - Auth state management
- `src/composables/useAuth.ts` - NEW - Auth composable
- `src/utils/api.ts` - NEW - Axios with JWT interceptor
- `src/components/LoginButton.vue` - NEW
- `src/components/UserMenu.vue` - NEW
- `src/views/AuthCallback.vue` - NEW
- `src/App.vue` - Added auth header
- `src/router/index.ts` - Added auth guards and callback route
- `src/services/api.ts` - Uses new api client

## Next Steps

1. Set up GitHub OAuth App (Step 1 above)
2. Configure backend with GitHub credentials (Step 2)
3. Run database migration (Step 3)
4. Start both applications (Step 4)
5. Test the authentication flow (Step 5)

That's it! Your application now has full GitHub OAuth authentication.
