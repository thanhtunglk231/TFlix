# TFlix - Agent Development Rules

## Project Overview

TFlix là nền tảng xem phim trực tuyến (OTT Video Streaming Platform) được xây dựng bằng ASP.NET Core.

Agent phải luôn coi đây là một sản phẩm thực tế đang được phát triển theo kiến trúc nhiều tầng.

Không được xem đây là project demo.

---

## Solution Structure

TFlix/
├── CommonLib
├── CoreLib
├── DataServiceLib
├── Server
├── WebBrowser
├── docker-compose.yml
└── TFlix.sln

---

## Project Responsibilities

### WebBrowser

Frontend MVC, chứa Controllers, Views, ViewModels, Razor Pages, JavaScript, CSS và Bootstrap UI. Đây là website người dùng cuối.

### Server

REST API Backend, chứa Authentication API, Movie API, Series API, Payment API, Streaming API và Admin API. Không chứa giao diện.

### CoreLib

Business Layer, chứa Services, DTO, Business Rules, Validation và Domain Logic. Mọi nghiệp vụ phải ưu tiên xử lý tại đây.

### DataServiceLib

Data Access Layer, chứa Repository, Database Query, Stored Procedure, EF Core hoặc ADO.NET. Không viết business logic tại đây.

### CommonLib

Shared Library, bao gồm Helpers, Extensions, Constants, Shared Models và Utility Classes.

---

## Database Domain

Database sử dụng SQL Server.

### Authentication

Tables: app_users, roles, user_roles, user_devices, user_auth_providers.

Features: Email Login, OAuth Login, Multi Device Login, User Roles.

### Content Management

Tables: movies, series, seasons, episodes.

Content Types: Movie, TV Series, Season, Episode.

### Classification

Tables: genres, countries, languages.

### Cast & Crew

Tables: people, movie_people, episode_people.

Jobs: ACTOR, DIRECTOR, WRITER, PRODUCER.

### Media Assets

Tables: movie_assets, series_assets, episode_assets, home_banners.

Asset Types: Poster, Backdrop, Thumbnail, Trailer.

### Video Streaming

Tables: video_sources, video_source_parts, subtitles.

Supported: HLS, DASH, MP4.

Video Providers: Cloudflare Stream, Bunny, Supabase Storage, Custom CDN.

Quality: 360p, 480p, 720p, 1080p, 4K.

### User Features

Tables: watch_progress, view_events, user_list_items, ratings, comments.

Features: Continue Watching, Favorite, Watchlist, Rating, Comment.

### Premium Subscription

Tables: subscription_plans, subscriptions, payments, payment_providers.

Supported Plans: BASIC, PREMIUM, FAMILY.

Supported Payments: Momo, VNPay, Stripe, PayPal.

### Notifications

Tables: notifications, user_notifications.

Types: System, New Movie, New Episode, Marketing.

### Advertisement

Tables: ad_campaigns, ad_creatives, ad_placements, ad_impressions.

### CMS

Tables: posts, post_categories, post_tags.

Features: News, Blog, Announcement.

### Forum

Tables: forum_categories, forum_threads, forum_posts.

Features: Community Discussion, User Interaction.

### Customer Support

Tables: support_tickets, support_messages, support_attachments.

### Analytics

Tables: content_daily_stats, api_call_logs.

Metrics: Views, Watch Time, Rating, Revenue, User Activity.

---

## Development Rules

### Always Specify Files

Khi viết code phải ghi rõ đường dẫn file:

File:
WebBrowser/Controllers/MovieController.cs

hoặc:

File:
Server/Controllers/MovieApiController.cs

### Never Change Architecture

Không tự động đổi tên project, đổi namespace hàng loạt, đổi solution structure hoặc đổi database schema nếu chưa được yêu cầu.

### Backend Standards

- ASP.NET Core.
- Dependency Injection.
- Async/Await.
- Repository Pattern.
- Service Layer Pattern.

### Frontend Standards

- Razor View.
- Bootstrap 5.
- Responsive.
- Mobile First.

UI tham khảo: Netflix, Disney+, FPT Play, VieON, Galaxy Play. Không sao chép nguyên mẫu hoặc thương hiệu của các nền tảng này.

---

## UI/UX And Frontend Design Rules

### General Principles

- WebBrowser là frontend dành cho người dùng cuối.
- Ưu tiên trải nghiệm xem phim nhanh, rõ ràng và dễ sử dụng.
- Thiết kế Mobile First.
- Không xem giao diện là bản demo; mọi thành phần phải sẵn sàng mở rộng cho sản phẩm thực tế.
- Không thay đổi business logic hoặc API chỉ để phục vụ giao diện.
- Không hard-code dữ liệu phim trong Razor View nếu dữ liệu đã có từ API hoặc ViewModel.
- Trước khi tạo component mới phải tìm component tương tự đang tồn tại.

### Visual Style

- Dark theme là giao diện chính.
- Màu nền chính: #111318 hoặc tương đương.
- Màu card: #1c1f26.
- Chữ chính: #ffffff; chữ phụ: #9aa0aa.
- Màu nhấn có thể dùng xanh dương, xanh ngọc hoặc đỏ tùy module.
- Bảo đảm độ tương phản cao để dễ đọc.
- Không lạm dụng màu sắc, gradient hoặc animation.
- Border-radius, khoảng cách và kích thước component phải thống nhất.

### Layout

- Sử dụng Bootstrap 5 kết hợp CSS riêng của dự án.
- Ưu tiên Bootstrap Grid, Flexbox và CSS Grid.
- Container có max-width hợp lý, nội dung không bị dàn quá rộng.
- Không để xuất hiện horizontal scrollbar.
- Các section phải có khoảng cách rõ ràng.
- Tái sử dụng layout, partial view và ViewComponent hiện có.

### Header

- Responsive trên desktop, tablet và mobile.
- Menu hiện tại có trạng thái active rõ ràng.
- Có thể sticky khi cuộn.
- Logo phải có fallback dạng text hoặc SVG nếu ảnh lỗi.
- Ô tìm kiếm phải có placeholder, icon và trạng thái focus.
- Nút đăng nhập, đăng ký và avatar có hover, focus và disabled state.
- Mobile dùng hamburger menu hoặc Bootstrap offcanvas.

### Home Page

Trang chủ nên có Hero Banner, Phim hot, Phim mới cập nhật, Phim lẻ mới nhất, Phim bộ phổ biến, Tiếp tục xem, Phim được xem nhiều, Thể loại phim và Phim sắp chiếu.

Mỗi section phải có tiêu đề rõ ràng, nút Xem tất cả nếu cần, không lặp dữ liệu không cần thiết, loading state, empty state và error state.

### Hero Banner

- Ảnh nền chất lượng tốt, có lớp phủ gradient để nội dung dễ đọc.
- Hiển thị tên phim, mô tả, năm, thể loại, điểm đánh giá và nút xem phim.
- Có thể có nút Chi tiết phim, điều hướng slide và indicator.
- Nếu có carousel, tự động chuyển slide sau khoảng 5 giây và hỗ trợ keyboard navigation.
- Banner phải co giãn tốt trên mobile, không chiếm quá nhiều chiều cao.

### Movie Card

- Poster đúng tỷ lệ 2:3, không bị méo hoặc vỡ.
- Hình ảnh bắt buộc có alt text.
- Hiển thị tên phim, tên gốc, năm, quốc gia, chất lượng, Vietsub/Thuyết minh và điểm đánh giá khi có dữ liệu.
- Hover có thể hiển thị nút xem phim, yêu thích hoặc thông tin nhanh.
- Không phóng to quá mức làm ảnh hưởng card xung quanh.
- Card cùng danh sách phải đồng nhất kích thước.

Responsive movie grid: Desktop 5–6 card/hàng; Laptop 4–5; Tablet 3–4; Mobile 2 card/hàng.

### Movie Detail And Watch Page

- Trang chi tiết ưu tiên thông tin quan trọng và nút xem phim.
- Trang xem phim tập trung vào video, hạn chế thành phần gây xao nhãng.
- Video player responsive theo tỷ lệ 16:9.
- TV Series phải có danh sách tập, tập hiện tại, tập trước và tập tiếp theo.
- Hiển thị trạng thái loading, lỗi phát video và video không khả dụng.
- Không hard-code URL video trong Razor View nếu URL đến từ backend.
- Không expose thông tin nhạy cảm của video source ra client nếu không cần thiết.

### Forms And Interaction

- Form có label hoặc aria-label đầy đủ.
- Validation hiển thị gần field bị lỗi.
- Không chỉ dùng màu để biểu thị lỗi hoặc trạng thái.
- Button có hover, focus, active và disabled state.
- Thao tác thành công/thất bại hiển thị bằng toast hoặc alert.
- Modal có nút đóng, hỗ trợ Escape và không làm mất trạng thái trang.
- Không gửi form nhiều lần khi request trước chưa hoàn thành.

### Loading, Empty And Error States

Mỗi màn hình gọi API phải xử lý loading bằng skeleton/spinner, empty state rõ ràng, error state thân thiện, retry state khi phù hợp và không để trang trắng khi API thất bại.

### Accessibility

- Sử dụng semantic HTML.
- Hình ảnh có alt text.
- Button dùng thẻ button, không dùng div giả button.
- Link có nội dung mô tả rõ ràng.
- Hỗ trợ keyboard navigation và focus state rõ ràng.
- Modal, dropdown, carousel và offcanvas có aria attributes phù hợp.
- Bảo đảm tương phản màu tốt.
- Không tự động phát âm thanh hoặc video có tiếng.

### Performance

- Lazy loading cho ảnh ngoài viewport đầu tiên.
- Tối ưu kích thước ảnh.
- Không tải JavaScript/CSS không cần thiết trên từng trang.
- Hạn chế animation nặng.
- Không gọi lại API nếu dữ liệu đã có trong ViewModel hoặc cache hợp lệ.
- Danh sách lớn dùng pagination hoặc tải thêm dữ liệu.

### Razor And Bootstrap

- Logic nghiệp vụ không được viết trong Razor View.
- Razor View chỉ xử lý hiển thị và tương tác frontend.
- Dữ liệu đi qua ViewModel hoặc DTO.
- Không gọi database từ Razor View.
- Không viết CSS inline nếu có thể đặt trong file CSS module.
- JavaScript dài phải đặt trong file JavaScript phù hợp, không viết trực tiếp trong .cshtml.
- Tên class CSS rõ nghĩa và nhất quán.
- Khi thêm UI phải kiểm tra Views/Shared/_Layout.cshtml, partial view, CSS và JavaScript liên quan.

---

## Coding Standards

### C#

PascalCase:

public class MovieService
{
}

camelCase:

var movieId = 1;

Async:

public async Task<MovieDto> GetMovieAsync(long movieId)
{
}

---

## When Debugging

Agent must provide:

1. Root Cause
2. How To Reproduce
3. Fix
4. Complete Code

Không chỉ trả lời lý thuyết.

---

## Execution Policy

Sau mỗi thay đổi:

dotnet restore
dotnet build

Nếu có test:

dotnet test

Nếu build hoặc test fail: đọc lỗi, sửa lỗi, chạy lại. Lặp tối đa 10 lần.

---

# Git Push And Merge Rules

Khi người dùng yêu cầu push code hoặc merge vào nhánh khác:

- Kiểm tra nhánh hiện tại, remote, trạng thái worktree và các commit chưa có trên remote trước khi stage hoặc push.
- Chỉ stage các thay đổi thuộc phạm vi yêu cầu; không stage toàn bộ worktree nếu có thay đổi không liên quan.
- Không push các file `launchSettings.json`, `appsettings*.json`,file logging, hoặc database changes trong `Database/**` hay `dbchanges/**`, trừ khi người dùng cho phép rõ ràng.
- Trước khi push, kiểm tra danh sách file trong commit/diff sắp gửi và xác nhận các đường dẫn bị loại trừ không xuất hiện.
- Nếu commit chưa push đã chứa file bị loại trừ, không push nhánh đó nguyên trạng. Tạo commit sạch dựa trên remote hoặc hỏi người dùng trước khi viết lại lịch sử.
- Không force-push hoặc ghi đè lịch sử remote nếu chưa được người dùng yêu cầu rõ ràng.
- Sau khi merge/push, xác nhận commit và nhánh remote đích; báo rõ nếu file bị loại trừ đã có trên remote từ commit khác.

---

## Run Project For Review

Khi người dùng yêu cầu sửa tính năng, giao diện, API hoặc cần xem kết quả chạy thực tế, agent phải tự chạy dự án sau khi build thành công.

1. Chạy Server API:

dotnet run --project Server/Server.csproj --launch-profile http

URL Server: http://localhost:5180
Swagger: http://localhost:5180/swagger

2. Chạy WebBrowser MVC:

dotnet run --project WebBrowser/WebBrowser.csproj --launch-profile http

URL WebBrowser: http://localhost:5181

Phải chạy Server trước WebBrowser. Nếu port bận, chọn port khác bằng --urls và báo lại URL. Nếu process đang chạy sẵn, không chạy trùng; kiểm tra log và dùng URL hiện có. Không dừng server đang chạy trừ khi người dùng yêu cầu.

---

## Current Priority

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

## Database Development Rules

Không tự ý tạo cấu trúc mới. Khi thêm chức năng phải tìm module tương tự đang tồn tại và code theo đúng pattern đó.

### Stored Procedure Rules

Ưu tiên: tạo Stored Procedure, gọi SP từ DataServiceLib, trả Model/DTO về CoreLib, trả dữ liệu qua Service, sau đó Controller sử dụng Service.

Không viết SQL trực tiếp trong Controller.

### SQL Folder Structure

Database/StoredProcedures/
├── Movie
├── Series
├── User
├── Payment
├── Subscription
└── Video

Ví dụ: Database/StoredProcedures/Movie/usp_Movie_Search.sql.

---

## Backend Folder Structure

Ví dụ module Movie:

CoreLib/Models/Movie
CoreLib/Services/Movie
DataServiceLib/Repositories/Movie
Server/Controllers/MovieController
WebBrowser/Controllers/MovieController
WebBrowser/Models/Movie
WebBrowser/Views/Movie

---

## Existing Pattern First

Trước khi code:

1. Tìm module tương tự đang tồn tại.
2. Copy đúng pattern của module đó.
3. Chỉ sửa phần nghiệp vụ mới.

Không tự phát minh kiến trúc mới.

---

## New Feature Checklist

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

## UI Review Checklist

Sau mỗi thay đổi giao diện phải kiểm tra:

- Desktop, tablet và mobile.
- Header và menu.
- Hero banner.
- Movie card.
- Modal và form.
- Loading, empty và error state.
- Không có lỗi console nghiêm trọng.
- Không có horizontal scrollbar.
- Không làm hỏng API hoặc chức năng hiện có.
- Không phá vỡ layout của các trang khác.

---

## Output Format

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