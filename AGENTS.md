# TFlix

## Project Overview

TFlix là nền tảng xem phim trực tuyến (OTT Video Streaming Platform) được xây dựng bằng ASP.NET Core.

Agent phải luôn coi đây là một sản phẩm thực tế đang được phát triển theo kiến trúc nhiều tầng.

Không được xem đây là project demo.

---

# Solution Structure

```text
TFlix
│
├── CommonLib
├── CoreLib
├── DataServiceLib
├── Server
├── WebBrowser
│
├── docker-compose.yml
└── TFlix.sln
```

---

# Project Responsibilities

## WebBrowser

Frontend MVC.

Chứa:

- Controllers
- Views
- ViewModels
- Razor Pages
- JavaScript
- CSS
- Bootstrap UI

Đây là website người dùng cuối.

---

## Server

REST API Backend.

Chứa:

- Authentication API
- Movie API
- Series API
- Payment API
- Streaming API
- Admin API

Không chứa giao diện.

---

## CoreLib

Business Layer.

Chứa:

- Services
- DTO
- Business Rules
- Validation
- Domain Logic

Mọi nghiệp vụ phải ưu tiên xử lý tại đây.

---

## DataServiceLib

Data Access Layer.

Chứa:

- Repository
- Database Query
- Stored Procedure
- EF Core hoặc ADO.NET

Không viết business logic tại đây.

---

## CommonLib

Shared Library.

Bao gồm:

- Helpers
- Extensions
- Constants
- Shared Models
- Utility Classes

---

# Database Domain

Database sử dụng SQL Server.

## Authentication

Tables:

- app_users
- roles
- user_roles
- user_devices
- user_auth_providers

Features:

- Email Login
- OAuth Login
- Multi Device Login
- User Roles

---

## Content Management

Tables:

- movies
- series
- seasons
- episodes

Content Types:

- Movie
- TV Series
- Season
- Episode

---

## Classification

Tables:

- genres
- countries
- languages

---

## Cast & Crew

Tables:

- people
- movie_people
- episode_people

Jobs:

- ACTOR
- DIRECTOR
- WRITER
- PRODUCER

---

## Media Assets

Tables:

- movie_assets
- series_assets
- episode_assets
- home_banners

Asset Types:

- Poster
- Backdrop
- Thumbnail
- Trailer

---

## Video Streaming

Tables:

- video_sources
- video_source_parts
- subtitles

Supported:

- HLS
- DASH
- MP4

Video Providers:

- Cloudflare Stream
- Bunny
- Supabase Storage
- Custom CDN

Quality:

- 360p
- 480p
- 720p
- 1080p
- 4K

---

## User Features

Tables:

- watch_progress
- view_events
- user_list_items
- ratings
- comments

Features:

- Continue Watching
- Favorite
- Watchlist
- Rating
- Comment

---

## Premium Subscription

Tables:

- subscription_plans
- subscriptions
- payments
- payment_providers

Supported Plans:

- BASIC
- PREMIUM
- FAMILY

Supported Payments:

- Momo
- VNPay
- Stripe
- PayPal

---

## Notifications

Tables:

- notifications
- user_notifications

Types:

- System
- New Movie
- New Episode
- Marketing

---

## Advertisement

Tables:

- ad_campaigns
- ad_creatives
- ad_placements
- ad_impressions

---

## CMS

Tables:

- posts
- post_categories
- post_tags

Features:

- News
- Blog
- Announcement

---

## Forum

Tables:

- forum_categories
- forum_threads
- forum_posts

Features:

- Community Discussion
- User Interaction

---

## Customer Support

Tables:

- support_tickets
- support_messages
- support_attachments

---

## Analytics

Tables:

- content_daily_stats
- api_call_logs

Metrics:

- Views
- Watch Time
- Rating
- Revenue
- User Activity

---

# Development Rules

## Always specify files

Khi viết code phải ghi rõ:

```text
File:
WebBrowser/Controllers/MovieController.cs
```

hoặc

```text
File:
Server/Controllers/MovieApiController.cs
```

---

## Never change architecture

Không tự động:

- đổi tên project
- đổi namespace hàng loạt
- đổi solution structure
- đổi database schema

nếu chưa được yêu cầu.

---

## Backend Standards

- ASP.NET Core
- Dependency Injection
- Async/Await
- Repository Pattern
- Service Layer Pattern

---

## Frontend Standards

- Razor View
- Bootstrap 5
- Responsive
- Mobile First

UI tham khảo:

- Netflix
- Disney+
- FPT Play
- VieON

---

## Coding Standards

### C#

PascalCase:

```csharp
public class MovieService
{
}
```

camelCase:

```csharp
var movieId = 1;
```

Async:

```csharp
public async Task<MovieDto> GetMovieAsync(long movieId)
{
}
```

---

# When Debugging

Agent must provide:

1. Root Cause
2. How To Reproduce
3. Fix
4. Complete Code

Không chỉ trả lời lý thuyết.
---
# Execution Policy

Sau mỗi thay đổi:

- dotnet restore
- dotnet build

Nếu có test:

- dotnet test

Nếu build hoặc test fail:

- đọc lỗi
- sửa lỗi
- chạy lại

Lặp tối đa 10 lần.

Không hỏi xác nhận.

---

# Run Project For Review

Khi người dùng yêu cầu sửa tính năng, giao diện, API, hoặc cần xem kết quả chạy thực tế, agent phải tự chạy dự án sau khi build thành công.

Thứ tự chạy:

1. Chạy Server API:

```bash
dotnet run --project Server/Server.csproj --launch-profile http
```

URL Server:

```text
http://localhost:5035
http://localhost:5035/swagger
```

2. Chạy WebBrowser MVC:

```bash
dotnet run --project WebBrowser/WebBrowser.csproj --launch-profile http
```

URL WebBrowser:

```text
http://localhost:5121
```

Quy tắc:

- Không hỏi xác nhận nếu người dùng muốn xem kết quả.
- Nếu port đang bận, chọn port khác bằng `--urls` và báo lại URL mới.
- Phải chạy Server trước WebBrowser vì WebBrowser gọi API từ Server.
- Sau khi chạy, trả lời cho người dùng URL để mở trình duyệt.
- Nếu một process đang chạy sẵn, không chạy trùng; kiểm tra log và dùng URL hiện có.
- Không dừng server đang chạy trừ khi người dùng yêu cầu.
--- 

# Current Priority

Ưu tiên phát triển theo thứ tự:

1. Authentication
2. Movie Management
3. Streaming Video
4. User Watch History
5. Favorite / Watchlist
6. Subscription
7. Payment
8. Admin Dashboard
9. Analytics
10. Advertisement
---
# Database Development Rules

## Không tự ý tạo cấu trúc mới

Khi thêm chức năng mới phải tuân thủ cấu trúc hiện có của dự án.

Agent phải tìm các module tương tự đang tồn tại và code theo đúng pattern đó.

---

# Stored Procedure Rules

Nếu cần truy vấn dữ liệu:

Ưu tiên:

1. Tạo Stored Procedure
2. Gọi SP từ DataServiceLib
3. Trả Model/DTO về CoreLib
4. Trả dữ liệu qua Service
5. Controller sử dụng Service

Không viết SQL trực tiếp trong Controller.

---

# SQL Folder Structure

Nếu tạo Stored Procedure mới:

Tạo file theo cấu trúc:

```text
Database
└── StoredProcedures
    ├── Movie
    ├── Series
    ├── User
    ├── Payment
    ├── Subscription
    └── Video
```

Ví dụ:

```text
Database
└── StoredProcedures
    └── Movie
        ├── usp_Movie_Search.sql
        ├── usp_Movie_Insert.sql
        ├── usp_Movie_Update.sql
        └── usp_Movie_Delete.sql
```

---

# Backend Folder Structure

Ví dụ module Movie:

```text
CoreLib
└── Models
    └── Movie

CoreLib
└── Services
    └── Movie

DataServiceLib
└── Repositories
    └── Movie

Server
└── Controllers
    └── MovieController

WebBrowser
├── Controllers
│   └── MovieController
│
├── Models
│   └── Movie
│
└── Views
    └── Movie
```

---

# Existing Pattern First

Trước khi code:

1. Tìm module tương tự đang tồn tại.
2. Copy đúng pattern của module đó.
3. Chỉ sửa phần nghiệp vụ mới.

Không tự phát minh kiến trúc mới.

---

# New Feature Checklist

Khi tạo chức năng mới phải tạo đầy đủ:

- SQL/SP
- Repository
- Service
- DTO/Model
- API Controller
- MVC Controller
- Razor View

Nếu thiếu bước nào phải nêu rõ lý do.

---

# Output Format

Luôn trả lời:

File mới:
- xxx

File sửa:
- xxx

Stored Procedure:
- xxx

Build command:
- dotnet build

Run command:
- dotnet run