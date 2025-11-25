# 🚀 Quick Start Guide

Hướng dẫn nhanh để chạy Chat Room System trong 5 phút!

## ⚡ Option 1: Run Locally (Recommended for Development)

### Bước 1: Cài đặt .NET SDK
Nếu chưa có .NET 8 SDK, tải tại: https://dotnet.microsoft.com/download/dotnet/8.0

Kiểm tra:
```bash
dotnet --version
# Should show 8.0.x
```

### Bước 2: Clone và Build
```bash
git clone <repo-url>
cd multiroom-chat-system

# Restore dependencies
dotnet restore

# Build solution
dotnet build
```

### Bước 3: Run Backend API
```bash
cd src/ChatRoomSystem.Api
dotnet run
```

✅ API sẽ chạy tại `http://localhost:5000`
✅ Swagger UI tại `http://localhost:5000`
✅ Database `chatroom.db` sẽ tự động được tạo

### Bước 4: Test với Swagger
1. Mở browser: `http://localhost:5000`
2. Test API flow:
   - `POST /api/auth/register` - Tạo account
   - `POST /api/auth/login` - Login và copy JWT token
   - Click nút **Authorize** ở góc trên, paste token
   - Test các endpoints khác

### Bước 5: Test WebSocket
Mở browser console (F12) và chạy:
```javascript
// Replace <TOKEN> với JWT token từ login
const ws = new WebSocket('ws://localhost:5000/ws?token=<TOKEN>');

ws.onopen = () => {
    console.log('✅ Connected!');

    // Send ping
    ws.send(JSON.stringify({
        type: 'Ping',
        timestamp: new Date().toISOString()
    }));
};

ws.onmessage = (event) => {
    const msg = JSON.parse(event.data);
    console.log('📨 Received:', msg);
};

ws.onerror = (error) => {
    console.error('❌ Error:', error);
};
```

Nếu thấy "✅ Connected!" và nhận được Pong → WebSocket hoạt động!

---

## 🐳 Option 2: Run với Docker (Production-like)

### Bước 1: Install Docker
Tải Docker Desktop: https://www.docker.com/products/docker-desktop

### Bước 2: Build và Run
```bash
cd multiroom-chat-system

# Build và start containers
docker-compose up --build -d

# Check logs
docker-compose logs -f api
```

✅ API sẽ chạy tại `http://localhost:5000`

### Stop Containers
```bash
docker-compose down
```

---

## 📱 Testing Scenarios

### Scenario 1: Create Room & Chat
1. **Register 2 users**:
   ```bash
   # User 1
   curl -X POST http://localhost:5000/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{"username":"alice","email":"alice@test.com","password":"Pass123"}'

   # User 2
   curl -X POST http://localhost:5000/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{"username":"bob","email":"bob@test.com","password":"Pass123"}'
   ```

2. **Login và lấy tokens**:
   ```bash
   # Login Alice
   curl -X POST http://localhost:5000/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"alice@test.com","password":"Pass123"}'
   # Copy token từ response
   ```

3. **Create room (Alice)**:
   ```bash
   curl -X POST http://localhost:5000/api/rooms \
     -H "Authorization: Bearer <ALICE_TOKEN>" \
     -H "Content-Type: application/json" \
     -d '{"name":"General Chat","description":"Main discussion","topic":"General","isPrivate":false}'
   # Copy roomId từ response
   ```

4. **Join room (Bob)**:
   ```bash
   curl -X POST http://localhost:5000/api/rooms/<ROOM_ID>/join \
     -H "Authorization: Bearer <BOB_TOKEN>"
   ```

5. **Connect WebSockets và chat**:
   - Mở 2 browser windows
   - Window 1: Connect WebSocket với Alice token
   - Window 2: Connect WebSocket với Bob token
   - Both join cùng room
   - Send messages qua WebSocket
   - Verify both users nhận được messages!

### Scenario 2: Friend Request Flow
1. **Alice search Bob**:
   ```bash
   curl -X GET "http://localhost:5000/api/friends/search?query=bob" \
     -H "Authorization: Bearer <ALICE_TOKEN>"
   ```

2. **Alice send friend request**:
   ```bash
   curl -X POST http://localhost:5000/api/friends/request \
     -H "Authorization: Bearer <ALICE_TOKEN>" \
     -H "Content-Type: application/json" \
     -d '{"toUserId":"<BOB_USER_ID>"}'
   ```

3. **Bob check pending requests**:
   ```bash
   curl -X GET http://localhost:5000/api/friends/requests \
     -H "Authorization: Bearer <BOB_TOKEN>"
   ```

4. **Bob accept request**:
   ```bash
   curl -X POST http://localhost:5000/api/friends/requests/<REQUEST_ID>/respond \
     -H "Authorization: Bearer <BOB_TOKEN>" \
     -H "Content-Type: application/json" \
     -d '{"requestId":"<REQUEST_ID>","accept":true}'
   ```

5. **Verify friendship**:
   ```bash
   curl -X GET http://localhost:5000/api/friends \
     -H "Authorization: Bearer <ALICE_TOKEN>"
   ```

### Scenario 3: Task Board (Trello-like)
1. **Create task trong room**:
   ```bash
   curl -X POST http://localhost:5000/api/tasks \
     -H "Authorization: Bearer <TOKEN>" \
     -H "Content-Type: application/json" \
     -d '{
       "roomId":"<ROOM_ID>",
       "title":"Implement Login UI",
       "description":"Create login page with form validation",
       "priority":2,
       "assigneeId":"<USER_ID>",
       "dueDate":"2025-12-31T00:00:00Z",
       "tags":["frontend","ui"]
     }'
   ```

2. **Update task status** (move to InProgress):
   ```bash
   curl -X PUT http://localhost:5000/api/tasks/<TASK_ID> \
     -H "Authorization: Bearer <TOKEN>" \
     -H "Content-Type: application/json" \
     -d '{"status":1}'
   ```

3. **Add comment**:
   ```bash
   curl -X POST http://localhost:5000/api/tasks/<TASK_ID>/comments \
     -H "Authorization: Bearer <TOKEN>" \
     -H "Content-Type: application/json" \
     -d '{"taskId":"<TASK_ID>","content":"Started working on this!"}'
   ```

4. **Get all tasks**:
   ```bash
   curl -X GET http://localhost:5000/api/tasks/room/<ROOM_ID> \
     -H "Authorization: Bearer <TOKEN>"
   ```

---

## 🎯 Next Steps

Sau khi backend chạy thành công:

### 1. Implement Blazor Frontend
Tham khảo README.md section "Frontend (Blazor) - TODO" để:
- Tạo WebSocket service
- Build UI components
- Implement real-time updates

### 2. Add Video Call
- Integrate WebRTC
- Handle peer connections
- Implement signaling via WebSocket

### 3. Add Whiteboard
- HTML5 Canvas drawing
- Sync strokes real-time
- Export/import sessions

### 4. Deploy Production
- Thay đổi JWT SecretKey
- Setup HTTPS với SSL cert
- Configure reverse proxy (nginx)
- Setup monitoring & logging

---

## 🐛 Common Issues

### Issue: Port 5000 already in use
**Solution**:
```bash
# Kill process on port 5000
# macOS/Linux:
lsof -ti:5000 | xargs kill -9

# Windows:
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

### Issue: Database migration error
**Solution**:
```bash
cd src/ChatRoomSystem.Api
dotnet ef database drop --force
dotnet ef database update
```

### Issue: WebSocket connection failed
**Solution**:
- Verify token chưa expired (default 24h)
- Check API đang chạy
- Ensure ws:// protocol (not wss:// for local)

### Issue: NuGet restore failed
**Solution**:
```bash
dotnet nuget locals all --clear
dotnet restore --force
```

---

## 📊 Database Inspection

View database contents:
```bash
# Install sqlite3 if needed
# macOS: brew install sqlite3
# Ubuntu: sudo apt-get install sqlite3

# Open database
sqlite3 src/ChatRoomSystem.Api/chatroom.db

# Show tables
.tables

# Query users
SELECT Id, UserName, Email, IsOnline FROM AspNetUsers;

# Query rooms
SELECT Id, Name, Topic, IsPrivate, CreatedAt FROM Rooms;

# Query messages
SELECT Id, RoomId, Content, Timestamp FROM Messages LIMIT 10;

# Exit
.quit
```

---

## 🎉 Success Checklist

- [ ] Backend API chạy thành công
- [ ] Swagger UI load được
- [ ] Có thể register/login users
- [ ] JWT token generation hoạt động
- [ ] WebSocket connection thành công
- [ ] Có thể send/receive messages qua WS
- [ ] Database lưu trữ data correctly
- [ ] Room join/leave hoạt động
- [ ] Friend system hoạt động
- [ ] Task board CRUD hoạt động

Nếu tất cả checked ✅ → Backend complete! 🎊

---

**Need Help?** Check README.md hoặc open an issue!
