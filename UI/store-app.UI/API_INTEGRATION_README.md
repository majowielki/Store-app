# API Integration Configuration

This document explains the updated axios configuration and Redux slices for your Store App microservices architecture.

## 🔗 API Configuration

### Base URL Configuration
The axios configuration has been updated to use the correct API Gateway port:

- **Development**: `http://localhost:5000/api`
- **Production**: `https://your-api-gateway.azurewebsites.net/api`

### Environment Files
- `.env.development` - Development environment settings
- `.env.production` - Production environment settings  
- `.env.local` - Local overrides
- `.env.example` - Template for other developers

## 🔐 Authentication

### User Slice (`src/features/user/userSlice.ts`)

**Async Actions:**
- `loginUserAsync(credentials)` - Login with email/password
- `registerUserAsync(userData)` - Register new user
- `logoutUserAsync()` - Logout current user
- `getCurrentUserAsync()` - Get current user info

**Regular Actions:**
- `loginUser()` - Legacy action for backward compatibility
- `logoutUser()` - Clear user state
- `clearError()` - Clear error state
- `setUser()` - Set user data
- `setToken()` - Set auth token

**Usage Example:**
```typescript
import { useDispatch } from 'react-redux';
import { loginUserAsync } from '@/features/user/userSlice';

const dispatch = useDispatch();

const handleLogin = async () => {
  try {
    await dispatch(loginUserAsync({
      email: 'user@example.com',
      password: 'password123'
    }));
  } catch (error) {
    console.error('Login failed:', error);
  }
};
```

## 🛒 Cart Management

### Cart Slice (`src/features/cart/cartSlice.ts`)

**Async Actions (Server Integration):**
- `fetchCart()` - Fetch cart from server
- `addItemToServer(item)` - Add item to server cart
- `updateItemOnServer({ itemId, quantity })` - Update item quantity
- `removeItemFromServer(itemId)` - Remove item from server
- `clearCartOnServer()` - Clear entire cart on server

**Local Actions (Offline Support):**
- `addItem()` - Add item locally
- `removeItem()` - Remove item locally
- `editItem()` - Edit item quantity locally
- `clearCart()` - Clear cart locally
- `calculateTotals()` - Recalculate totals
- `syncWithServer()` - Sync local state with server

**Usage Example:**
```typescript
import { useDispatch } from 'react-redux';
import { addItemToServer } from '@/features/cart/cartSlice';

const dispatch = useDispatch();

const handleAddToCart = async () => {
  try {
    await dispatch(addItemToServer({
      productId: 1,
      quantity: 2,
      color: 'red'
    }));
  } catch (error) {
    console.error('Failed to add item:', error);
  }
};
```

## 🌐 API Endpoints

Your API Gateway routes the following endpoints:

- **Auth**: `/api/auth/*` → Identity Service (port 5001)
- **Products**: `/api/products/*` → Product Service (port 5002)  
- **Cart**: `/api/cart/*` → Cart Service (port 5003)
- **Orders**: `/api/orders/*` → Order Service (port 5004)

## 🛠 Development Setup

1. **Start your services** using Docker Compose:
   ```bash
   docker-compose up -d
   ```

2. **API Gateway** will be available at: `http://localhost:5000`

3. **Frontend** connects to the gateway automatically using environment variables

## 🔄 Migration from Old Code

### For existing cart operations:
- Replace direct `addItem()` calls with `addItemToServer()` for server sync
- Use `fetchCart()` on app startup to sync with server
- Keep local actions for offline support

### For authentication:
- Replace old login logic with `loginUserAsync()`
- Update user state access to use the new `UserState` interface
- Token is now automatically handled by axios interceptors

## 🚀 Azure Deployment

When deploying to Azure:

1. Update `.env.production` with your actual Azure API Gateway URL
2. Set environment variables in your Azure Static Web App or App Service
3. The configuration will automatically switch to production mode

## ⚡ Features

- **Automatic token management** - JWT tokens are automatically added to requests
- **Error handling** - Comprehensive error handling with user-friendly messages
- **Offline support** - Local cart actions work without server connection
- **Server synchronization** - Seamless sync between local and server state
- **Type safety** - Full TypeScript support with proper type definitions
- **Backward compatibility** - Existing code continues to work during migration

## 📁 File Structure

```
src/
├── features/
│   ├── user/
│   │   └── userSlice.ts          # User authentication & state
│   └── cart/
│       └── cartSlice.ts          # Cart management & state
├── utils/
│   ├── api.ts                    # API service functions
│   ├── customFetch.ts            # Axios configuration
│   ├── errorHandling.ts          # Error handling utilities
│   └── types.ts                  # TypeScript type definitions
└── components/
    └── ApiUsageExample.tsx       # Usage examples
```

Your application is now configured to work seamlessly with your microservices architecture! 🎉
