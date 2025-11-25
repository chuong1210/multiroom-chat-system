# 🎨 Frontend Implementation Guide - Blazor

## ✅ Đã Complete

### Services (100%)
- ✅ **WebSocketService** - Real-time WebSocket client
  - Connect/disconnect với JWT token
  - Send/receive messages
  - Event-based architecture (OnMessageReceived, OnUserJoined, etc.)
  - Automatic reconnection handling
  - Heartbeat ping/pong

- ✅ **AuthService** - Authentication management
  - Register/Login/Logout
  - JWT token storage (localStorage)
  - Auto authentication check
  - HttpClient authorization header management

- ✅ **ApiService** - HTTP API calls
  - Rooms API (get, create, join, leave, search)
  - Messages API (history with pagination)
  - Friends API (list, search, requests)
  - Tasks API (CRUD, comments)

### Pages (Core Complete)
- ✅ **Home** (`/`) - Auto redirect to `/rooms` or `/login`
- ✅ **Login** (`/login`) - Login/Register form
  - Beautiful gradient background
  - Toggle between login/register modes
  - Form validation
  - Error/success messages

- ✅ **Rooms** (`/rooms`) - Room list và management
  - Tabs: My Rooms / Public Rooms
  - Search functionality
  - Create room modal
  - Room cards với hover effects
  - Member count và online status

- ✅ **ChatRoom** (`/room/{id}`) - Real-time chat room
  - WebSocket connection với auto-reconnect
  - Message history loading
  - Real-time message updates
  - Typing indicators
  - User join/leave notifications
  - Send messages (Enter to send)
  - Connection status badge

### Configuration
- ✅ appsettings.json với API URLs
- ✅ Blazored.LocalStorage integration
- ✅ Dependency injection setup
- ✅ HttpClient base address configuration

## 🚀 Cách Chạy Frontend

### Option 1: Development Mode
```bash
# Terminal 1: Run Backend API
cd src/ChatRoomSystem.Api
dotnet run
# API sẽ chạy tại http://localhost:5000

# Terminal 2: Run Blazor Client
cd src/ChatRoomSystem.Client
dotnet run
# Client sẽ chạy tại http://localhost:5001
```

### Option 2: Docker Compose (Recommended)
```bash
# Build và run tất cả services
docker-compose up --build

# API: http://localhost:5000
# Client: http://localhost:5001 (nếu uncomment trong docker-compose.yml)
```

## 📱 User Flow

### 1. First Time User
1. Access `http://localhost:5001`
2. Auto redirect to `/login`
3. Click "Chưa có tài khoản? Đăng ký ngay"
4. Enter username, email, password
5. Click "Đăng ký" → Auto login → Redirect to `/rooms`

### 2. Login Flow
1. Access `/login`
2. Enter email và password
3. Click "Đăng nhập"
4. Redirect to `/rooms`

### 3. Create & Join Room
1. In `/rooms`, click "+ Tạo Room"
2. Enter room name, description, topic
3. Choose public/private
4. Click "Tạo Room" → Auto join room
5. Or click any existing room card to join

### 4. Chat in Room
1. In `/room/{id}`, WebSocket auto connects
2. See "● Connected" badge in header
3. Type message in input box
4. Press Enter or click "Gửi"
5. See real-time messages từ other users
6. See typing indicators khi others typing

## 🎨 Features Implemented

### Real-time Features
- ✅ Instant message delivery (WebSocket)
- ✅ Typing indicators (2s timeout)
- ✅ User online/offline status
- ✅ User join/leave room notifications
- ✅ Auto-reconnect khi disconnect
- ✅ Connection status indicator

### UI/UX Features
- ✅ Responsive design
- ✅ Beautiful gradients và shadows
- ✅ Smooth transitions và hover effects
- ✅ Loading spinners
- ✅ Error/success messages
- ✅ Modal dialogs
- ✅ Form validation
- ✅ Auto-scroll messages (planned)

### Security Features
- ✅ JWT authentication
- ✅ Protected routes (redirect to login)
- ✅ Token storage trong localStorage
- ✅ Authorization headers cho API calls
- ✅ Token validation trong WebSocket handshake

## ⏳ Pending Features (Optional Enhancements)

### Pages
- ⏳ `/friends` - Friends management page
  - Friends list với online status
  - Search users
  - Send friend requests
  - Accept/reject requests

- ⏳ `/tasks/{roomId}` - Task board (Kanban)
  - 3 columns: ToDo, InProgress, Done
  - Drag-drop tasks
  - Create/edit/delete tasks
  - Assign to members
  - Add comments
  - Real-time updates

- ⏳ `/profile` - User profile settings
  - Update username/avatar
  - Change password
  - View joined rooms

### Components
- ⏳ **MessageList.razor** - Scrollable message list
  - Virtual scrolling cho performance
  - Load more (pagination)
  - Message search
  - Copy message
  - Delete message (own messages)

- ⏳ **RoomSidebar.razor** - Members sidebar
  - Member list với avatars
  - Online/offline status
  - Click to send private message
  - Admin actions (kick, promote)

- ⏳ **TaskBoard.razor** - Kanban board component
  - Drag-drop between columns
  - Task cards với priority colors
  - Due date warnings
  - Assignee avatars

- ⏳ **TypingIndicator.razor** - Animated typing dots
  - 3 dots animation
  - Multiple users typing

- ⏳ **OnlineBadge.razor** - Online status badge
  - Green dot for online
  - Gray dot for offline
  - Last seen time

- ⏳ **NotificationToast.razor** - Toast notifications
  - Friend requests
  - Room invitations
  - Mentions (@user)
  - Auto-dismiss

### Advanced Features
- ⏳ **File Upload** - Share images/files
  - Drag-drop upload
  - Image preview trong chat
  - File size limits
  - Progress bar

- ⏳ **Emoji Picker** - Add emojis
  - Emoji selector
  - Recent emojis
  - Emoji reactions to messages

- ⏳ **Message Search** - Search trong room
  - Full-text search
  - Filter by sender
  - Date range

- ⏳ **Dark Mode** - Theme toggle
  - Dark/light theme switch
  - Save preference
  - CSS variables

- ⏳ **Notifications** - Browser notifications
  - New messages
  - Mentions
  - Friend requests
  - Permission request

- ⏳ **Video Call** - WebRTC integration
  - Peer-to-peer connection
  - Video/audio streams
  - Mute/unmute controls
  - Screen share during call

- ⏳ **Whiteboard** - HTML5 Canvas
  - Drawing tools
  - Color picker
  - Eraser
  - Save/export

## 🐛 Troubleshooting

### Issue: WebSocket connection failed
**Symptoms**: "○ Disconnected" badge, no real-time updates

**Solutions**:
1. Check backend API đang chạy: `http://localhost:5000/health`
2. Check WebSocket URL trong appsettings.json: `ws://localhost:5000/ws`
3. Check JWT token valid (not expired)
4. Check console logs cho errors
5. Try logout và login lại

### Issue: API calls fail (401 Unauthorized)
**Symptoms**: Cannot load rooms, cannot send messages

**Solutions**:
1. Check JWT token có trong localStorage: F12 → Application → Local Storage
2. Logout và login lại để get new token
3. Check backend API running
4. Check network tab cho error details

### Issue: Messages not appearing
**Symptoms**: Send message nhưng không thấy trong chat

**Solutions**:
1. Check WebSocket connected (green badge)
2. Check console logs
3. Refresh page
4. Check backend logs: `docker-compose logs -f api`

### Issue: Typing indicator stuck
**Symptoms**: "User đang typing..." không biến mất

**Solutions**:
1. Reload page
2. Check WebSocket connection
3. Issue sẽ auto-clear sau 2s

### Issue: Cannot create room
**Symptoms**: Modal closes nhưng room không xuất hiện

**Solutions**:
1. Check backend logs
2. Check network tab (F12)
3. Verify all required fields filled
4. Check database connection

## 🎯 Next Steps to Complete

### High Priority
1. **Auto-scroll to bottom** - Messages container
   - Implement JS interop
   - Scroll on new message
   - "Scroll to bottom" button

2. **Message pagination** - Load more button
   - Load older messages
   - Infinite scroll
   - Loading indicator

3. **Friends page** - Complete friends management
   - Full UI implementation
   - Real-time friend request notifications

4. **Task board** - Trello-like kanban
   - Drag-drop library integration
   - Real-time task updates

### Medium Priority
5. **Room sidebar** - Members list
6. **Profile page** - User settings
7. **File upload** - Image/file sharing
8. **Emoji picker** - Emoji support
9. **Dark mode** - Theme toggle

### Low Priority
10. **Video call** - WebRTC implementation
11. **Whiteboard** - Canvas drawing
12. **Notifications** - Browser push
13. **Message search** - Full-text search

## 📚 Code Structure

```
src/ChatRoomSystem.Client/
├── Services/
│   ├── WebSocketService.cs      # WebSocket client
│   ├── AuthService.cs            # Authentication
│   └── ApiService.cs             # API calls
│
├── Components/
│   ├── Pages/
│   │   ├── Home.razor            # "/" - Redirect
│   │   ├── Login.razor           # "/login" - Auth
│   │   ├── Rooms.razor           # "/rooms" - Room list
│   │   ├── ChatRoom.razor        # "/room/{id}" - Chat
│   │   ├── Friends.razor         # TODO
│   │   ├── TaskBoard.razor       # TODO
│   │   └── Profile.razor         # TODO
│   │
│   ├── Shared/                   # Shared components
│   │   ├── MessageList.razor     # TODO
│   │   ├── RoomSidebar.razor     # TODO
│   │   └── TaskCard.razor        # TODO
│   │
│   └── Layout/
│       ├── MainLayout.razor      # Default layout
│       └── NavMenu.razor         # Navigation
│
├── wwwroot/
│   ├── app.css                   # Global styles
│   └── bootstrap/                # Bootstrap CSS
│
├── Program.cs                    # DI configuration
└── appsettings.json              # Configuration
```

## 🎨 Styling Guide

### Theme Colors
```css
--primary: #4A90E2;      /* Blue - Buttons, links */
--secondary: #7B68EE;    /* Purple - Accents */
--success: #50C878;      /* Green - Success states */
--background: #F0F8FF;   /* Light blue - Page background */
--text: #333333;         /* Dark gray - Text */
--border: #E0E0E0;       /* Light gray - Borders */
```

### Component Patterns
- **Cards**: `border-radius: 10px`, `box-shadow: 0 2px 8px rgba(0,0,0,0.1)`
- **Buttons**: `border-radius: 5px`, `transition: all 0.3s`
- **Hover**: `transform: translateY(-2px)`, `box-shadow: 0 5px 15px rgba(0,0,0,0.2)`
- **Modals**: `background: rgba(0,0,0,0.5)`, centered

## 🔐 Security Best Practices

1. **Token Storage**: JWT stored trong localStorage (OK for SPA)
2. **Token Expiry**: 24h default, auto logout on expire
3. **HTTPS**: Use HTTPS trong production
4. **Input Validation**: Validate trước khi API call
5. **XSS Protection**: Blazor auto-escapes content
6. **CSRF**: Anti-forgery tokens enabled

## 🚀 Performance Tips

1. **Lazy Loading**: Load messages on scroll
2. **Virtual Scrolling**: For large message lists
3. **Debouncing**: Typing indicators (2s)
4. **Caching**: Room list caching
5. **WebSocket**: Reuse single connection
6. **Image Optimization**: Compress uploads

## 📖 Resources

- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [WebSocket API](https://developer.mozilla.org/en-US/docs/Web/API/WebSocket)
- [Blazored LocalStorage](https://github.com/Blazored/LocalStorage)
- [Bootstrap 5](https://getbootstrap.com/docs/5.0/)

---

**Frontend Progress**: ~60% complete (Core features working)
**Estimated Time to 100%**: 2-3 days

**Current Status**: ✅ Fully functional chat system với real-time WebSocket!
