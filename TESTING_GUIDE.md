# 🧪 Testing Guide - Chat Room System

## ✅ Complete Application Testing

### Prerequisites
- .NET 8 SDK installed
- Ports 5000 (API) và 5001 (Client) available
- Terminal/Command Prompt

## 🚀 Quick Start (5 Minutes)

### Step 1: Run Backend API
```bash
# Terminal 1
cd src/ChatRoomSystem.Api
export PATH="$HOME/.dotnet:$PATH"  # Linux/Mac
dotnet run
```

**Expected Output:**
```
=================================================
  Chat Room System API
=================================================
  Environment: Development
  API URL: http://localhost:5000
  Swagger UI: http://localhost:5000
  WebSocket: ws://localhost:5000/ws?token=<jwt>
=================================================
```

✅ **Verify**: Open `http://localhost:5000` trong browser → Should see Swagger UI

### Step 2: Run Blazor Frontend
```bash
# Terminal 2 (new terminal window)
cd src/ChatRoomSystem.Client
export PATH="$HOME/.dotnet:$PATH"  # Linux/Mac
dotnet run
```

**Expected Output:**
```
=================================================
  Chat Room System - Blazor Client
=================================================
  Client URL: http://localhost:5001
  API Backend: http://localhost:5000
=================================================
```

✅ **Verify**: Open `http://localhost:5001` → Should redirect to Login page

---

## 📝 Test Scenarios

### Scenario 1: User Registration & Login (2 minutes)

#### Test Case 1.1: Register New User
1. Access `http://localhost:5001`
2. Should auto-redirect to `/login`
3. Click "Chưa có tài khoản? Đăng ký ngay"
4. Fill form:
   - Username: `testuser1`
   - Email: `test1@example.com`
   - Password: `Pass123`
5. Click "Đăng ký"

**Expected Results:**
- ✅ Green success message: "Đăng ký thành công!"
- ✅ Auto-redirect to `/rooms` after 0.5s
- ✅ See "👋 testuser1" trong header

#### Test Case 1.2: Register Duplicate User (Error Handling)
1. Logout (click "Đăng xuất")
2. Try register same email again
3. Click "Đăng ký"

**Expected Results:**
- ❌ Red error message: "Email đã được sử dụng"
- ✅ Stay on login page

#### Test Case 1.3: Login Existing User
1. Click "Đã có tài khoản? Đăng nhập"
2. Fill form:
   - Email: `test1@example.com`
   - Password: `Pass123`
3. Click "Đăng nhập"

**Expected Results:**
- ✅ Green success message: "Đăng nhập thành công!"
- ✅ Redirect to `/rooms`
- ✅ JWT token saved trong localStorage (F12 → Application → Local Storage)

#### Test Case 1.4: Invalid Login
1. Try login với wrong password
2. Fill:
   - Email: `test1@example.com`
   - Password: `wrongpass`
3. Click "Đăng nhập"

**Expected Results:**
- ❌ Red error: "Email hoặc password không đúng"
- ✅ Stay on login page

---

### Scenario 2: Room Management (3 minutes)

#### Test Case 2.1: Create Public Room
1. In `/rooms`, click "+ Tạo Room"
2. Fill modal:
   - Tên Room: `General Discussion`
   - Mô tả: `Main chat for everyone`
   - Chủ đề: `General`
   - Private Room: ☐ (unchecked)
3. Click "Tạo Room"

**Expected Results:**
- ✅ Modal closes
- ✅ Auto-redirect to `/room/{id}` (chat room page)
- ✅ See room name "General Discussion" trong header

#### Test Case 2.2: Create Private Room
1. Click "← Back" to return to `/rooms`
2. Click "+ Tạo Room"
3. Fill:
   - Tên Room: `Private Team`
   - Mô tả: `Team only`
   - Chủ đề: `Work`
   - Private Room: ☑ (checked)
4. Click "Tạo Room"

**Expected Results:**
- ✅ Room created với 🔒 badge
- ✅ Redirect to room

#### Test Case 2.3: View My Rooms
1. Click "← Back"
2. Should be on "My Rooms" tab
3. See both rooms created:
   - "General Discussion" (no lock)
   - "Private Team" (🔒 badge)

**Expected Results:**
- ✅ 2 room cards visible
- ✅ Member count: "1 members (1 online)"
- ✅ Each card shows name, description, topic

#### Test Case 2.4: Search Rooms
1. In search box, type: `general`
2. See filtered results in real-time

**Expected Results:**
- ✅ Only "General Discussion" visible
- ✅ "Private Team" hidden
- ✅ Clear search → Both rooms visible again

#### Test Case 2.5: Switch to Public Rooms Tab
1. Click "Public Rooms" tab
2. See list of all public rooms

**Expected Results:**
- ✅ "General Discussion" visible
- ✅ "Private Team" NOT visible (it's private)

---

### Scenario 3: Real-time Chat (5 minutes) ⭐

**⚠️ Important**: This test requires 2 browser windows (or 2 different browsers)

#### Setup: Create 2 Users
1. **Browser 1**: Register/Login as `user1@test.com` (testuser1)
2. **Browser 2**: Incognito/Private window, Register/Login as `user2@test.com` (testuser2)

#### Test Case 3.1: Join Same Room
**Browser 1 (User1):**
1. Go to `/rooms`
2. Click "General Discussion" room

**Browser 2 (User2):**
1. Go to `/rooms` → "Public Rooms" tab
2. Click "General Discussion" room

**Expected Results:**
- ✅ Both users see "● Connected" badge
- ✅ User1 sees system message: "testuser2 joined" (in console)
- ✅ Member count updates to "2 members"

#### Test Case 3.2: Send Messages
**Browser 1 (User1):**
1. Type: `Hello everyone!`
2. Press Enter (or click "Gửi")

**Browser 2 (User2):**
1. **Instantly** see User1's message appear

**Expected Results:**
- ✅ Message appears in both browsers instantly
- ✅ User1: Message aligned right (blue background)
- ✅ User2: Message aligned left (white background)
- ✅ Timestamp shown (HH:mm format)

**Browser 2 (User2):**
1. Type: `Hi! This is amazing!`
2. Press Enter

**Expected Results:**
- ✅ Both users see User2's message instantly
- ✅ Messages stack vertically with proper alignment

#### Test Case 3.3: Typing Indicators
**Browser 1 (User1):**
1. Start typing in input box (don't send yet)
2. Just type some characters...

**Browser 2 (User2):**
1. Watch bottom of messages area

**Expected Results:**
- ✅ User2 sees: "testuser1 đang typing..." (appears after typing starts)
- ✅ Indicator disappears 2 seconds after User1 stops typing
- ✅ User1 does NOT see own typing indicator

#### Test Case 3.4: Multiple Messages Flow
**Browser 1 & 2**: Exchange messages quickly
1. User1: `How are you?`
2. User2: `I'm good, thanks!`
3. User1: `Great to hear!`

**Expected Results:**
- ✅ All messages appear instantly
- ✅ Proper ordering (chronological)
- ✅ Timestamps accurate
- ✅ No delays or lag

#### Test Case 3.5: User Leave Room
**Browser 2 (User2):**
1. Click "← Back" to leave room

**Browser 1 (User1):**
1. Watch for notification

**Expected Results:**
- ✅ User1 sees system message (in console): "testuser2 left"
- ✅ Member count updates to "1 member"

---

### Scenario 4: Message History & Pagination (2 minutes)

#### Test Case 4.1: Load Message History
**Setup**: Send 10+ messages trong room (use same browser, send multiple times)

1. Send 10 messages: `Message 1`, `Message 2`, ..., `Message 10`
2. Click "← Back" to leave room
3. Click room again to rejoin

**Expected Results:**
- ✅ All 10 messages loaded from database
- ✅ Messages display in correct order (oldest first)
- ✅ Timestamps preserved

---

### Scenario 5: Authentication Persistence (1 minute)

#### Test Case 5.1: Refresh Page
1. In `/rooms`, press F5 (refresh page)

**Expected Results:**
- ✅ Still logged in (no redirect to login)
- ✅ Username still shows in header
- ✅ Rooms still visible

#### Test Case 5.2: Close & Reopen Browser
1. Close entire browser
2. Reopen browser
3. Go to `http://localhost:5001`

**Expected Results:**
- ✅ Auto-redirect to `/rooms` (not `/login`)
- ✅ Still authenticated
- ✅ JWT token loaded from localStorage

#### Test Case 5.3: Logout
1. Click "Đăng xuất" button
2. Should redirect to `/login`
3. Try accessing `/rooms` directly: `http://localhost:5001/rooms`

**Expected Results:**
- ✅ Redirected back to `/login`
- ✅ Cannot access protected routes
- ✅ Token removed from localStorage

---

### Scenario 6: WebSocket Reconnection (2 minutes)

#### Test Case 6.1: API Restart During Chat
**Setup**: Have chat room open với "● Connected"

1. **Keep Browser 1 open** on chat room
2. **In Terminal 1** (Backend API):
   - Press `Ctrl+C` to stop API
   - Wait 5 seconds
   - Run `dotnet run` again

3. **Back to Browser 1**:
   - Watch connection status badge

**Expected Results:**
- ✅ Badge changes: "● Connected" → "○ Disconnected"
- ✅ After API restarts: Auto-reconnect (badge → "● Connected")
- ✅ Can send messages again after reconnect

---

### Scenario 7: Multiple Rooms & Navigation (2 minutes)

#### Test Case 7.1: Switch Between Rooms
1. Create 3 rooms: "Room A", "Room B", "Room C"
2. Join "Room A", send message: `Hello from Room A`
3. Click "← Back", join "Room B", send: `Hello from Room B`
4. Click "← Back", join "Room A" again

**Expected Results:**
- ✅ "Room A" shows previous message: `Hello from Room A`
- ✅ Messages from "Room B" NOT visible in "Room A"
- ✅ Each room maintains separate message history
- ✅ Connection status persists across navigation

---

## 🐛 Common Issues & Solutions

### Issue 1: API Connection Refused
**Symptoms**: Cannot load rooms, 5xx errors

**Solution**:
```bash
# Check API is running
curl http://localhost:5000/health

# If not running, restart API
cd src/ChatRoomSystem.Api
dotnet run
```

### Issue 2: WebSocket Won't Connect
**Symptoms**: "○ Disconnected" badge always red

**Solutions**:
1. Check JWT token valid:
   - F12 → Application → Local Storage
   - Should see `authToken` key
   - Logout và login lại

2. Check WebSocket URL:
   - Open `src/ChatRoomSystem.Client/appsettings.json`
   - Verify: `"WebSocketUrl": "ws://localhost:5000/ws"`

3. Check API logs:
   - Look for WebSocket connection errors
   - Look for authentication failures

### Issue 3: Messages Not Appearing
**Symptoms**: Send message, doesn't show up

**Solutions**:
1. Check WebSocket connected (green badge)
2. Check browser console (F12 → Console) for errors
3. Refresh page
4. Check API terminal for errors

### Issue 4: Typing Indicator Stuck
**Symptoms**: "User đang typing..." never disappears

**Solution**: Reload page (will auto-clear after 2s anyway)

### Issue 5: Cannot Create Room
**Symptoms**: Modal closes, no room created

**Solutions**:
1. Check all fields filled
2. Check API terminal for errors
3. Check Network tab (F12) for 400/500 errors

---

## 📊 Test Results Template

```
=== CHAT ROOM SYSTEM TEST RESULTS ===
Date: _____________
Tester: _____________

Scenario 1: User Registration & Login
[ ] TC 1.1: Register New User .......................... PASS / FAIL
[ ] TC 1.2: Duplicate Registration Error ............... PASS / FAIL
[ ] TC 1.3: Login Existing User ........................ PASS / FAIL
[ ] TC 1.4: Invalid Login .............................. PASS / FAIL

Scenario 2: Room Management
[ ] TC 2.1: Create Public Room ......................... PASS / FAIL
[ ] TC 2.2: Create Private Room ........................ PASS / FAIL
[ ] TC 2.3: View My Rooms .............................. PASS / FAIL
[ ] TC 2.4: Search Rooms ............................... PASS / FAIL
[ ] TC 2.5: Switch Tabs ................................ PASS / FAIL

Scenario 3: Real-time Chat ⭐
[ ] TC 3.1: Join Same Room ............................. PASS / FAIL
[ ] TC 3.2: Send Messages .............................. PASS / FAIL
[ ] TC 3.3: Typing Indicators .......................... PASS / FAIL
[ ] TC 3.4: Multiple Messages .......................... PASS / FAIL
[ ] TC 3.5: User Leave Room ............................ PASS / FAIL

Scenario 4: Message History
[ ] TC 4.1: Load History ............................... PASS / FAIL

Scenario 5: Authentication Persistence
[ ] TC 5.1: Refresh Page ............................... PASS / FAIL
[ ] TC 5.2: Close & Reopen Browser ..................... PASS / FAIL
[ ] TC 5.3: Logout ..................................... PASS / FAIL

Scenario 6: WebSocket Reconnection
[ ] TC 6.1: API Restart During Chat .................... PASS / FAIL

Scenario 7: Multiple Rooms
[ ] TC 7.1: Switch Between Rooms ....................... PASS / FAIL

Overall Result: _____ / 19 tests passed

Notes:
_____________________________________________
_____________________________________________
_____________________________________________
```

---

## 🎯 Success Criteria

Application is considered **FULLY FUNCTIONAL** if:

- ✅ All test cases PASS
- ✅ Real-time chat works flawlessly
- ✅ WebSocket reconnection automatic
- ✅ No console errors during normal flow
- ✅ JWT authentication secure
- ✅ Message history persists
- ✅ Multiple users can chat simultaneously

---

## 🚀 Performance Benchmarks

**Expected Performance:**
- Message send/receive latency: < 100ms
- Page load time: < 2s
- WebSocket connection time: < 500ms
- API response time: < 200ms
- No memory leaks after 1 hour usage

---

**Happy Testing! 🎉**

If all tests pass, congratulations! You have a **production-ready real-time chat system**! 🚀
