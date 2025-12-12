-- Users Table
CREATE TABLE Users (
    Id NVARCHAR(450) PRIMARY KEY,
    UserName NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FullName NVARCHAR(200),
    AvatarUrl NVARCHAR(500),
    Bio NVARCHAR(500),
    IsOnline BIT DEFAULT 0,
    LastSeen DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Rooms Table
CREATE TABLE Rooms (
    Id NVARCHAR(450) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000),
    IsPrivate BIT DEFAULT 0,
    CreatedById NVARCHAR(450) NOT NULL,
    AvatarUrl NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (CreatedById) REFERENCES Users(Id) ON DELETE NO ACTION
);

-- RoomMembers Table (Many-to-Many: Users <-> Rooms)
CREATE TABLE RoomMembers (
    Id NVARCHAR(450) PRIMARY KEY,
    RoomId NVARCHAR(450) NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    Role NVARCHAR(50) DEFAULT 'Member', -- 'Owner', 'Admin', 'Member'
    JoinedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE(RoomId, UserId)
);

-- Messages Table
CREATE TABLE Messages (
    Id NVARCHAR(450) PRIMARY KEY,
    RoomId NVARCHAR(450) NOT NULL,
    SenderId NVARCHAR(450) NOT NULL,
    Content NVARCHAR(MAX),
    MessageType NVARCHAR(50) DEFAULT 'Text', -- 'Text', 'Image', 'Video', 'File'
    FileUrl NVARCHAR(500),
    FileName NVARCHAR(255),
    FileSize BIGINT,
    IsDeleted BIT DEFAULT 0,
    IsRead BIT DEFAULT 0,
    ReadAt DATETIME2,
    Timestamp DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE,
    FOREIGN KEY (SenderId) REFERENCES Users(Id) ON DELETE NO ACTION
);

-- Friendships Table
CREATE TABLE Friendships (
    Id NVARCHAR(450) PRIMARY KEY,
    RequesterId NVARCHAR(450) NOT NULL,
    ReceiverId NVARCHAR(450) NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Pending', -- 'Pending', 'Accepted', 'Rejected'
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (RequesterId) REFERENCES Users(Id) ON DELETE NO ACTION,
    FOREIGN KEY (ReceiverId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CHECK (RequesterId != ReceiverId),
    UNIQUE(RequesterId, ReceiverId)
);

-- JoinRequests Table
CREATE TABLE JoinRequests (
    Id NVARCHAR(450) PRIMARY KEY,
    RoomId NVARCHAR(450) NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Pending', -- 'Pending', 'Approved', 'Rejected'
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE(RoomId, UserId)
);

-- Notifications Table
CREATE TABLE Notifications (
    Id NVARCHAR(450) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    Type NVARCHAR(50) NOT NULL, -- 'FriendRequest', 'JoinRequest', 'Message', 'Mention'
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(1000),
    IsRead BIT DEFAULT 0,
    RelatedUserId NVARCHAR(450),
    RelatedRoomId NVARCHAR(450),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RelatedUserId) REFERENCES Users(Id) ON DELETE NO ACTION,
    FOREIGN KEY (RelatedRoomId) REFERENCES Rooms(Id) ON DELETE NO ACTION
);

-- TypingIndicators Table (Optional - for real-time typing status)
CREATE TABLE TypingIndicators (
    Id NVARCHAR(450) PRIMARY KEY,
    RoomId NVARCHAR(450) NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    IsTyping BIT DEFAULT 1,
    LastTypingAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE(RoomId, UserId)
);

-- Indexes for Performance
CREATE INDEX IX_Messages_RoomId ON Messages(RoomId);
CREATE INDEX IX_Messages_SenderId ON Messages(SenderId);
CREATE INDEX IX_Messages_Timestamp ON Messages(Timestamp DESC);
CREATE INDEX IX_RoomMembers_RoomId ON RoomMembers(RoomId);
CREATE INDEX IX_RoomMembers_UserId ON RoomMembers(UserId);
CREATE INDEX IX_Friendships_RequesterId ON Friendships(RequesterId);
CREATE INDEX IX_Friendships_ReceiverId ON Friendships(ReceiverId);
CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
CREATE INDEX IX_JoinRequests_RoomId ON JoinRequests(RoomId);
CREATE INDEX IX_JoinRequests_UserId ON JoinRequests(UserId);