# 🕐 PiClock

> Ứng dụng đồng hồ kỹ thuật số hiện đại cho Raspberry Pi với slideshow ảnh và thông tin thời tiết

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![Avalonia](https://img.shields.io/badge/Avalonia-11.3-8B44AC?style=flat-square)
![Platform](https://img.shields.io/badge/Platform-Raspberry%20Pi-C51A4A?style=flat-square&logo=raspberry-pi)

## ✨ Tính năng

- ⏰ **Đồng hồ kỹ thuật số** - Hiển thị giờ, phút, ngày tháng năm (tiếng Việt)
- 🌤️ **Thông tin thời tiết** - Tự động cập nhật từ Open-Meteo API (Vĩnh Long)
- 🖼️ **Slideshow ảnh** - Tự động chuyển ảnh mỗi 10 giây với hiệu ứng Ken Burns
- 💬 **Tích hợp Telegram Bot** - Nhận và hiển thị tin nhắn trực tiếp từ Telegram
- 🎨 **Giao diện đẹp mắt** - Thiết kế hiện đại với font Inter & JetBrains Mono
- 🔔 **Thông báo dạng Toast** - Hiển thị tối đa 3 tin nhắn với hiệu ứng Glass Morphism
- 🔄 **Tự động rotate ảnh** - Xử lý EXIF orientation
- 💾 **Tiết kiệm tài nguyên** - Tối ưu cho Raspberry Pi

## 📋 Yêu cầu hệ thống

- 🥧 Raspberry Pi 3/4/5 hoặc tương đương
- 💿 Raspbian OS (Debian 11/12 trở lên)
- 📦 .NET 9.0 Runtime
- 🖥️ Môi trường desktop (X11)
- 🌐 Kết nối internet (cho thời tiết và Telegram)

## 🚀 Hướng dẫn cài đặt trên Raspbian

### Bước 1: Cài đặt .NET 9.0 Runtime

```bash
# Tải script cài đặt .NET
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --runtime dotnet

# Thêm vào PATH
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc

# Kiểm tra cài đặt
dotnet --version
```

### Bước 2: Cài đặt các gói phụ thuộc

```bash
# Cài đặt thư viện đồ họa cần thiết cho Avalonia
sudo apt-get update
sudo apt-get install -y \
    libice6 \
    libsm6 \
    libfontconfig1 \
    libx11-6 \
    libx11-xcb1 \
    libxcursor1 \
    libxext6 \
    libxi6 \
    libxrandr2
```

### Bước 3: Tải ứng dụng

```bash
# Tạo thư mục cài đặt
sudo mkdir -p /opt/piclock
cd /opt/piclock

# Giải nén file build (thay thế bằng file build thực tế của bạn)
# Hoặc copy từ thư mục publish-pi/
sudo cp -r /path/to/publish-pi/* /opt/piclock/

# Cấp quyền thực thi
sudo chmod +x /opt/piclock/PiClock
```

### Bước 4: Tạo file cấu hình

```bash
# Tạo thư mục config
sudo mkdir -p /opt/piclock/Config

# Tạo file config
sudo nano /opt/piclock/Config/AppConfig.json
```

Thêm nội dung sau (điều chỉnh theo nhu cầu):

```json
{
  "Location": {
    "Latitude": 10.0668,
    "Longitude": 105.9088,
    "Name": "Vĩnh Long, Việt Nam"
  },
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "CheckIntervalSeconds": 5,
    "MaxVisibleMessages": 3
  },
  "Slideshow": {
    "IntervalSeconds": 10,
    "ImageFolder": "images",
    "KenBurnsAnimationSeconds": 20
  },
  "Weather": {
    "UpdateIntervalMinutes": 30
  }
}
```

### Bước 5: Cấu hình Telegram Bot (Tùy chọn)

Nếu muốn nhận thông báo từ Telegram:

1. Mở Telegram, tìm `@BotFather`
2. Gửi lệnh `/newbot` và làm theo hướng dẫn
3. Copy Bot Token nhận được
4. Mở file config và thay `YOUR_BOT_TOKEN_HERE` bằng token thực

```bash
sudo nano /opt/piclock/Config/AppConfig.json
# Sửa dòng: "BotToken": "1234567890:ABCdefGHI..."
```

### Bước 6: Tạo thư mục ảnh

```bash
# Tạo thư mục images
sudo mkdir -p /opt/piclock/images

# Copy ảnh của bạn vào thư mục này
sudo cp /path/to/your/photos/*.jpg /opt/piclock/images/

# Cấp quyền đọc
sudo chmod -R 755 /opt/piclock/images
```

## 🔧 Tạo dịch vụ systemd (Khởi động cùng hệ thống)

### Tạo file service

```bash
sudo nano /etc/systemd/system/piclock.service
```

Thêm nội dung sau:

```ini
[Unit]
Description=PiClock - Digital Clock with Slideshow
After=graphical.target network-online.target
Wants=graphical.target network-online.target

[Service]
Type=simple
User=pi
Environment="DISPLAY=:0"
Environment="DOTNET_ROOT=/home/pi/.dotnet"
WorkingDirectory=/opt/piclock
ExecStart=/opt/piclock/PiClock
Restart=on-failure
RestartSec=5

[Install]
WantedBy=graphical.target
```

**📝 Lưu ý:** Thay `pi` bằng username của bạn nếu khác.

### Kích hoạt dịch vụ

```bash
# Reload systemd
sudo systemctl daemon-reload

# Kích hoạt dịch vụ
sudo systemctl enable piclock.service

# Khởi động dịch vụ
sudo systemctl start piclock.service

# Kiểm tra trạng thái
sudo systemctl status piclock.service
```

### Các lệnh quản lý dịch vụ

```bash
# Khởi động
sudo systemctl start piclock

# Dừng
sudo systemctl stop piclock

# Khởi động lại
sudo systemctl restart piclock

# Xem log
sudo journalctl -u piclock -f

# Vô hiệu hóa khởi động cùng hệ thống
sudo systemctl disable piclock
```

## ⚙️ Cấu hình

### File cấu hình JSON

Ứng dụng sử dụng file `Config/AppConfig.json` để quản lý tất cả cấu hình:

```json
{
  "Location": {
    "Latitude": 10.0668,
    "Longitude": 105.9088,
    "Name": "Vĩnh Long, Việt Nam"
  },
  "Telegram": {
    "BotToken": "BOT_TOKEN_HERE",
    "CheckIntervalSeconds": 5,
    "MaxVisibleMessages": 3
  },
  "Slideshow": {
    "IntervalSeconds": 10,
    "ImageFolder": "images",
    "KenBurnsAnimationSeconds": 20
  },
  "Weather": {
    "UpdateIntervalMinutes": 30
  }
}
```

**Chỉnh sửa file cấu hình:**

```bash
nano /opt/piclock/Config/AppConfig.json
```

### Cấu hình Telegram Bot

**Cách lấy Bot Token:**
1. Mở Telegram, tìm `@BotFather`
2. Gửi lệnh `/newbot`
3. Đặt tên và username cho bot
4. Copy token nhận được và paste vào `BotToken` trong file config

**Cách sử dụng:**
- Gửi tin nhắn bất kỳ đến bot → Hiện trên màn hình
- Gửi `/clear` → Xóa toàn bộ tin nhắn
- Hỗ trợ cả Group và Channel

### Các tham số cấu hình

| Tham số | Mô tả | Mặc định |
|---------|-------|----------|
| `Location.Latitude` | Vĩ độ vị trí | 10.0668 |
| `Location.Longitude` | Kinh độ vị trí | 105.9088 |
| `Telegram.BotToken` | Token bot Telegram | BOT_TOKEN_HERE |
| `Telegram.CheckIntervalSeconds` | Kiểm tra tin nhắn mới (giây) | 5 |
| `Telegram.MaxVisibleMessages` | Số tin nhắn tối đa hiển thị | 3 |
| `Slideshow.IntervalSeconds` | Thời gian chuyển ảnh (giây) | 10 |
| `Weather.UpdateIntervalMinutes` | Cập nhật thời tiết (phút) | 30 |

## 📁 Cấu trúc thư mục

```
/opt/piclock/
├── PiClock                    # File thực thi
├── PiClock.deps.json
├── PiClock.runtimeconfig.json
├── createdump
├── Assets/                    # Font Inter & JetBrains Mono
│   └── Fonts/
├── Config/                    # Thư mục cấu hình
│   └── AppConfig.json        # File cấu hình JSON
├── Models/                    # Data models
│   └── AppConfig.cs
├── Services/                  # Business logic
│   ├── TelegramService.cs
│   ├── WeatherService.cs
│   └── SlideshowService.cs
└── images/                    # Thư mục chứa ảnh slideshow
    ├── photo1.jpg
    ├── photo2.png
    └── ...
```

## 🏗️ Kiến trúc ứng dụng

### Cấu trúc code mới (Tối ưu)

**Models** - Chứa các class định nghĩa dữ liệu:
- `AppConfig.cs` - Quản lý cấu hình ứng dụng từ JSON

**Services** - Các service xử lý logic nghiệp vụ:
- `TelegramService.cs` - Kết nối và nhận tin nhắn Telegram
- `WeatherService.cs` - Lấy dữ liệu thời tiết từ API
- `SlideshowService.cs` - Quản lý slideshow ảnh

**MainWindow** - UI logic, kết hợp các service lại

### Ưu điểm của cấu trúc mới

✅ **Separation of Concerns** - Tách biệt rõ ràng giữa UI và logic  
✅ **Dễ bảo trì** - Mỗi service độc lập, dễ sửa lỗi  
✅ **Dễ test** - Có thể test từng service riêng  
✅ **Cấu hình linh hoạt** - Thay đổi config không cần rebuild  
✅ **Tái sử dụng** - Services có thể dùng cho các project khác

## 🖼️ Định dạng ảnh hỗ trợ

- ✅ JPEG/JPG
- ✅ PNG
- ✅ BMP
- ✅ WEBP

**Khuyến nghị:** Sử dụng ảnh có độ phân giải 1920x1080 hoặc tỷ lệ 16:9 để hiển thị tốt nhất.

## 🐛 Xử lý sự cố

### Ứng dụng không khởi động

```bash
# Kiểm tra log
sudo journalctl -u piclock -n 50

# Kiểm tra quyền
ls -la /opt/piclock/PiClock

# Thử chạy thủ công
cd /opt/piclock
./PiClock
```

### Không hiển thị giao diện

```bash
# Kiểm tra DISPLAY
echo $DISPLAY

# Cấp quyền X11
xhost +local:

# Kiểm tra thư viện
ldd /opt/piclock/PiClock
```

### Không có ảnh slideshow

```bash
# Kiểm tra thư mục images
ls -la /opt/piclock/images/

# Kiểm tra quyền
sudo chmod -R 755 /opt/piclock/images
```

### Telegram không hoạt động

```bash
# Kiểm tra log trong console
sudo journalctl -u piclock -f

# Xem output (tìm dòng "TELEGRAM KẾT NỐI THÀNH CÔNG")
# Nếu thấy "LỖI TELEGRAM", kiểm tra:
# 1. Token có đúng không (xóa khoảng trắng thừa)
# 2. Kết nối internet có ổn định không
# 3. Bot có bị block bởi Telegram không

# Test thủ công
curl https://api.telegram.org/bot<YOUR_TOKEN>/getMe
```

## 📊 Build từ source

```bash
# Clone repository
git clone <your-repo-url>
cd PiClock

# Build cho Linux ARM64
dotnet publish -c Release -r linux-arm64 --self-contained false

# Output tại: bin/Release/net9.0/linux-arm64/publish/
```

## 📝 License

MIT License

## 👤 Tác giả

Dự án PiClock

## 💡 Tính năng nổi bật

### 📱 Telegram Integration

Ứng dụng tích hợp Telegram Bot để nhận thông báo real-time:

- **Thông báo cá nhân**: Gửi tin nhắn từ bot đến màn hình
- **Hỗ trợ Group**: Thêm bot vào group để mọi người cùng gửi
- **Hỗ trợ Channel**: Forward tin nhắn từ channel
- **Quản lý tin nhắn**: Gửi `/clear` để xóa toàn bộ
- **Hiển thị đẹp mắt**: Glass Morphism effect với animation mượt
- **Tối đa 3 tin**: Tự động xóa tin cũ khi đầy

### 🎬 Ken Burns Effect

Hiệu ứng zoom và pan nhẹ nhàng trên ảnh nền (20 giây/chu kỳ)

### 🎨 Glass Morphism UI

Thông báo Telegram hiển thị với:
- Nền kính mờ (frosted glass)
- Viền gradient phát sáng
- Animation trượt và fade mượt mà
- Icon Telegram đẹp mắt

## 🙏 Cảm ơn

- [Avalonia UI](https://avaloniaui.net/) - Framework UI cross-platform
- [Open-Meteo](https://open-meteo.com/) - API thời tiết miễn phí
- [ImageSharp](https://sixlabors.com/products/imagesharp/) - Thư viện xử lý ảnh
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) - Telegram Bot API

---

<p align="center">Made with ❤️ for Raspberry Pi</p>
