# Chat Room System - Hệ Thống Chat Đa Phòng Full-Stack

Một ứng dụng web full-stack hoàn chỉnh cho hệ thống Chat Room với nhiều phòng trò chuyện theo chủ đề, được xây dựng hoàn toàn bằng C# và .NET 8, sử dụng WebSockets thuần túy cho real-time communication.

## 🌟 Tính Năng Chính

### Core Features
- ✅ **Multi-Room Chat**: Nhiều phòng chat theo chủ đề (public/private)
- ✅ **Real-time Communication**: WebSocket thuần túy (không dùng SignalR)
- ✅ **Authentication**: JWT tokens với ASP.NET Identity
- ✅ **Friend System**: Gửi/nhận friend requests, quản lý bạn bè
- ✅ **Private Chat**: Chat riêng 1-1 giữa friends
- ✅ **Group Management**: Tạo nhóm, mời thành viên, quản lý permissions
- ✅ **Message History**: Lưu trữ và pagination tin nhắn cũ
- ✅ **Online Status**: Tracking trạng thái online/offline real-time

### Advanced Features
- ✅ **Task Board (Trello-like)**: Kanban board trong mỗi room với drag-drop tasks
- ✅ **Typing Indicators**: Hiển thị khi user đang typing
- 🔧 **Screen Sharing**: Share màn hình với WebRTC
- 🔧 **Video Call**: 1-1 và group video calls
- 🔧 **Whiteboard**: Vẽ chung real-time trên canvas

✅ = Đã implement hoàn chỉnh (Backend)
🔧 = Cần implement Frontend/Integration

## 🏗️ Kiến Trúc Hệ Thống

### Technology Stack
- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: Blazor Server (hoặc WebAssembly)
- **Database**: SQLite với Entity Framework Core
- **Real-time**: WebSockets (Microsoft.AspNetCore.WebSockets)
- **Authentication**: JWT với ASP.NET Identity
- **Containerization**: Docker & Docker Compose

### Solution Structure
```
multiroom-chat-system/
├── src/
│   ├── ChatRoomSystem.Shared/        # Shared models/DTOs
│   │   └── Models/
│   │       ├── UserDto.cs
│   │       ├── RoomDto.cs
│   │       ├── MessageDto.cs
│   │       ├── FriendDto.cs
│   │       ├── TaskDto.cs
│   │       └── WebSocketDto.cs
│   │
│   ├── ChatRoomSystem.Data/          # Data layer với EF Core
│   │   ├── Entities/
│   │   │   ├── ApplicationUser.cs
│   │   │   ├── Room.cs
│   │   │   ├── RoomMember.cs
│   │   │   ├── Message.cs
│   │   │   ├── Friendship.cs
│   │   │   ├── FriendRequest.cs
│   │   │   ├── RoomInvitation.cs
│   │   │   ├── ChatRoomTask.cs
│   │   │   └── TaskComment.cs
│   │   └── ChatRoomDbContext.cs
│   │
│   ├── ChatRoomSystem.Api/           # Backend API
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── RoomsController.cs
│   │   │   ├── MessagesController.cs
│   │   │   ├── FriendsController.cs
│   │   │   └── TasksController.cs
│   │   ├── Services/
│   │   │   ├── WebSocketConnectionManager.cs
│   │   │   ├── WebSocketMessageHandler.cs
│   │   │   └── JwtService.cs
│   │   ├── Middleware/
│   │   │   └── WebSocketMiddleware.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   └── ChatRoomSystem.Client/        # Blazor frontend
│       ├── Pages/
│       ├── Components/
│       └── Services/
│
├── ChatRoomSystem.sln
├── docker-compose.yml
└── README.md
```

## 📦 Database Schema

### Tables
- **AspNetUsers** (Identity): User accounts với online status
- **Rooms**: Chat rooms/groups (public/private)
- **RoomMembers**: Many-to-many relationship User ↔ Room
- **Messages**: Tin nhắn (text, image, file, system)
- **Friendships**: Many-to-many relationship giữa Users
- **FriendRequests**: Lời mời kết bạn (pending/accepted/rejected)
- **RoomInvitations**: Lời mời vào room
- **Tasks**: Task board items (Kanban)
- **TaskComments**: Comments trên tasks

### Key Relationships
- User → CreatedRooms (1-to-many)
- User ↔ Rooms via RoomMembers (many-to-many)
- User ↔ Friends via Friendships (many-to-many)
- Room → Messages (1-to-many)
- Room → Tasks (1-to-many)
- Task → Comments (1-to-many)

## 🚀 Setup & Installation

### Prerequisites
- .NET 8 SDK
- Docker (optional, for containerization)
- SQLite (embedded, không cần install riêng)

### Step 1: Clone Repository
```bash
git clone <repository-url>
cd multiroom-chat-system
```

### Step 2: Install Dependencies
```bash
# Restore tất cả NuGet packages
dotnet restore
```

### Step 3: Database Migration
```bash
# Di chuyển đến API project
cd src/ChatRoomSystem.Api

# Tạo database migration (nếu chưa có)
dotnet ef migrations add InitialCreate --project ../ChatRoomSystem.Data

# Apply migrations
dotnet ef database update --project ../ChatRoomSystem.Data
```

**Lưu ý**: Database sẽ tự động được tạo khi chạy application lần đầu tiên (xem Program.cs).

### Step 4: Run Backend API
```bash
cd src/ChatRoomSystem.Api
dotnet run
```

Backend API sẽ chạy tại:
- API: `http://localhost:5000`
- Swagger UI: `http://localhost:5000`
- WebSocket: `ws://localhost:5000/ws?token=<jwt-token>`

### Step 5: Run Frontend (Blazor)
```bash
cd src/ChatRoomSystem.Client
dotnet run
```

Frontend sẽ chạy tại `http://localhost:5001`

### Step 6: Run với Docker Compose (Optional)
```bash
docker-compose up --build
```

## 🔧 Configuration

### JWT Settings (appsettings.json)
```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyHereShouldBeAtLeast32CharactersLong!",
    "Issuer": "ChatRoomSystem",
    "Audience": "ChatRoomSystem",
    "ExpiryMinutes": "1440"
  }
}
```

**⚠️ QUAN TRỌNG**: Thay đổi `SecretKey` trước khi deploy production!

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=chatroom.db"
  }
}
```

SQLite database file `chatroom.db` sẽ được tạo trong thư mục API.

## 🔌 API Endpoints

### Authentication
- `POST /api/auth/register` - Đăng ký user mới
- `POST /api/auth/login` - Đăng nhập
- `GET /api/auth/me` - Get current user info
- `POST /api/auth/logout` - Đăng xuất

### Rooms
- `GET /api/rooms/public` - Lấy danh sách public rooms
- `GET /api/rooms/my-rooms` - Lấy rooms của user
- `GET /api/rooms/{id}` - Chi tiết room
- `POST /api/rooms` - Tạo room mới
- `PUT /api/rooms/{id}` - Update room
- `POST /api/rooms/{id}/join` - Join room
- `POST /api/rooms/{id}/leave` - Leave room
- `GET /api/rooms/search?query=...` - Tìm kiếm rooms

### Messages
- `GET /api/messages/room/{roomId}?pageNumber=1&pageSize=50` - Lấy messages của room (pagination)
- `GET /api/messages/private/{userId}?limit=50` - Lấy private messages

### Friends
- `GET /api/friends` - Lấy danh sách bạn bè
- `POST /api/friends/request` - Gửi friend request
- `GET /api/friends/requests` - Lấy pending friend requests
- `POST /api/friends/requests/{id}/respond` - Accept/reject friend request
- `DELETE /api/friends/{id}` - Xóa bạn
- `GET /api/friends/search?query=...` - Tìm kiếm users

### Tasks
- `GET /api/tasks/room/{roomId}` - Lấy tasks của room
- `POST /api/tasks` - Tạo task mới
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Xóa task
- `POST /api/tasks/{id}/comments` - Add comment

## 📡 WebSocket Protocol

### Connection
```
ws://localhost:5000/ws?token=<jwt-token>
```

### Message Types
```typescript
enum WebSocketMessageType {
    // Authentication
    Authenticate,
    AuthenticationSuccess,
    AuthenticationFailed,

    // Chat
    ChatMessage,
    MessageReceived,

    // Room
    JoinRoom,
    LeaveRoom,
    RoomJoined,
    RoomLeft,
    UserJoinedRoom,
    UserLeftRoom,

    // Friends
    FriendRequest,
    FriendRequestReceived,
    FriendRequestAccepted,
    FriendRequestRejected,

    // Tasks
    TaskCreated,
    TaskUpdated,
    TaskDeleted,
    TaskCommentAdded,

    // Video/WebRTC
    CallInitiate,
    CallAccept,
    CallReject,
    CallEnd,
    WebRTCOffer,
    WebRTCAnswer,
    WebRTCIceCandidate,

    // Whiteboard
    WhiteboardDraw,
    WhiteboardClear,
    WhiteboardData,

    // Status
    UserOnline,
    UserOffline,
    UserTyping,
    UserStoppedTyping,

    // Heartbeat
    Ping,
    Pong,

    // Error
    Error
}
```

### Example Messages

**Send Chat Message:**
```json
{
    "type": "ChatMessage",
    "data": "{\"roomId\":\"room-123\",\"content\":\"Hello everyone!\",\"messageType\":0}",
    "timestamp": "2025-11-25T10:00:00Z"
}
```

**Join Room:**
```json
{
    "type": "JoinRoom",
    "data": "{\"roomId\":\"room-123\"}",
    "timestamp": "2025-11-25T10:00:00Z"
}
```

**Receive Message:**
```json
{
    "type": "MessageReceived",
    "data": "{\"id\":\"msg-456\",\"roomId\":\"room-123\",\"senderId\":\"user-789\",\"senderUsername\":\"john_doe\",\"content\":\"Hello!\",\"type\":0,\"timestamp\":\"2025-11-25T10:00:00Z\"}",
    "timestamp": "2025-11-25T10:00:00Z"
}
```

## 🎨 Frontend (Blazor) - TODO

Frontend Blazor cần implement:

### Services
- `WebSocketService.cs` - Quản lý WebSocket connection
- `AuthService.cs` - Authentication state management
- `RoomService.cs` - Room operations
- `MessageService.cs` - Message caching và pagination
- `TaskService.cs` - Task board state

### Pages
- `/login` - Login/Register page
- `/rooms` - Room list và search
- `/room/{id}` - Chat room page với sidebar
- `/friends` - Friends management
- `/tasks/{roomId}` - Task board (Kanban)

### Components
- `ChatWindow.razor` - Main chat area
- `MessageList.razor` - Scrollable messages
- `MessageInput.razor` - Text input với typing indicator
- `RoomSidebar.razor` - Members list
- `TaskBoard.razor` - Kanban board với drag-drop
- `WhiteboardCanvas.razor` - HTML5 Canvas cho whiteboard
- `VideoCallWindow.razor` - WebRTC video grid

### Styling
Theme color combo:
- Primary: `#4A90E2` (Blue)
- Secondary: `#7B68EE` (Purple)
- Accent: `#50C878` (Green)
- Background: `#F0F8FF` (Light blue)
- Text: `#333333`

CSS Framework: Bootstrap 5 hoặc Tailwind CSS

## 🐳 Docker Deployment

### Build Images
```bash
docker-compose build
```

### Run Containers
```bash
docker-compose up -d
```

### Stop Containers
```bash
docker-compose down
```

### View Logs
```bash
docker-compose logs -f api
```

## 🧪 Testing

### Manual Testing với Swagger
1. Chạy API: `dotnet run` trong `ChatRoomSystem.Api`
2. Mở browser: `http://localhost:5000`
3. Test endpoints:
   - Register user mới
   - Login để lấy JWT token
   - Click "Authorize" và paste token
   - Test các endpoints khác

### WebSocket Testing
Sử dụng tool như [websocat](https://github.com/vi/websocat):
```bash
# Install websocat
cargo install websocat

# Connect
websocat "ws://localhost:5000/ws?token=<your-jwt-token>"

# Send ping
{"type":"Ping","timestamp":"2025-11-25T10:00:00Z"}
```

Hoặc sử dụng JavaScript console trong browser:
```javascript
const ws = new WebSocket('ws://localhost:5000/ws?token=<your-jwt-token>');

ws.onopen = () => {
    console.log('Connected');
    // Send ping
    ws.send(JSON.stringify({
        type: 'Ping',
        timestamp: new Date().toISOString()
    }));
};

ws.onmessage = (event) => {
    console.log('Received:', JSON.parse(event.data));
};
```

## 📚 Technical Details

### WebSocket Connection Management
- **ConcurrentDictionary**: Thread-safe mapping userId → WebSocket
- **Room Groups**: Efficient broadcast per room
- **Heartbeat**: Ping/Pong every 30s để detect disconnections
- **Reconnection**: Auto-reconnect logic trong frontend
- **Message Queueing**: Buffer messages khi offline

### Security
- **JWT Validation**: Token required cho WebSocket handshake
- **Input Validation**: Sanitize user inputs chống XSS
- **SQL Injection Protection**: EF Core parameterized queries
- **Rate Limiting**: Throttle message sends (TODO)
- **HTTPS**: Self-signed cert cho local dev

### Performance Optimization
- **Async/Await**: Non-blocking I/O operations
- **Connection Pooling**: EF Core connection pool
- **Message Pagination**: Load 50 messages at a time
- **Index Optimization**: Database indexes trên foreign keys
- **Caching**: In-memory cache cho active users (TODO)

## 🤝 Contributing

### Các Task Cần Complete
1. **Frontend Blazor**:
   - Implement WebSocket client service
   - Create UI components theo design
   - Add responsive mobile layout
   - Implement drag-drop cho task board

2. **Video Call & Screen Sharing**:
   - Integrate WebRTC signaling
   - Handle peer-to-peer connections
   - Implement fallback cho TURN server

3. **Whiteboard**:
   - HTML5 Canvas drawing
   - Sync strokes real-time
   - Export/import whiteboard sessions

4. **Advanced Features**:
   - File upload/download
   - Image/video preview
   - Emoji reactions
   - Message search
   - Notifications (browser push)

## 📄 License

This project is licensed under the MIT License.

## 👥 Authors

Developed for educational purposes - CLO1.1, CLO1.2, CLO2.1, CLO2.2, CLO3.1, CLO3.2, CLO3.3

## 🆘 Troubleshooting

### Database Migration Errors
```bash
# Xóa migrations cũ
rm -rf src/ChatRoomSystem.Data/Migrations

# Tạo lại migration
cd src/ChatRoomSystem.Api
dotnet ef migrations add InitialCreate --project ../ChatRoomSystem.Data
dotnet ef database update --project ../ChatRoomSystem.Data
```

### WebSocket Connection Failed
- Kiểm tra JWT token có hợp lệ
- Verify token chưa expired
- Đảm bảo API đang chạy
- Check CORS settings nếu frontend khác domain

### Port Already in Use
```bash
# Thay đổi port trong Properties/launchSettings.json
# Hoặc kill process đang dùng port
lsof -ti:5000 | xargs kill -9  # macOS/Linux
netstat -ano | findstr :5000    # Windows
```

## 📞 Support

Nếu gặp vấn đề:
1. Kiểm tra logs: `docker-compose logs -f`
2. Verify dependencies: `dotnet restore`
3. Clear build: `dotnet clean && dotnet build`
4. Check database: `sqlite3 chatroom.db ".tables"`

---

**Happy Coding! 🚀**
