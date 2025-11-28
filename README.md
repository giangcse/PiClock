# 🕐 PiClock

> Ứng dụng đồng hồ kỹ thuật số hiện đại cho Raspberry Pi với slideshow ảnh và thông tin thời tiết

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![Avalonia](https://img.shields.io/badge/Avalonia-11.3-8B44AC?style=flat-square)
![Platform](https://img.shields.io/badge/Platform-Raspberry%20Pi-C51A4A?style=flat-square&logo=raspberry-pi)

## ✨ Tính năng

- ⏰ **Đồng hồ kỹ thuật số** - Hiển thị giờ, phút, ngày tháng năm (tiếng Việt)
- 🌤️ **Thông tin thời tiết** - Tự động cập nhật từ Open-Meteo API (Vĩnh Long)
- 🖼️ **Slideshow ảnh** - Tự động chuyển ảnh mỗi 10 giây với hiệu ứng fade
- 🎨 **Giao diện đẹp mắt** - Thiết kế hiện đại với font Inter
- 🔄 **Tự động rotate ảnh** - Xử lý EXIF orientation
- 💾 **Tiết kiệm tài nguyên** - Tối ưu cho Raspberry Pi

## 📋 Yêu cầu hệ thống

- 🥧 Raspberry Pi 3/4/5 hoặc tương đương
- 💿 Raspbian OS (Debian 11/12 trở lên)
- 📦 .NET 9.0 Runtime
- 🖥️ Môi trường desktop (X11)

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

### Bước 4: Tạo thư mục ảnh

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

### Thay đổi vị trí thời tiết

Mở file `MainWindow.axaml.cs` và chỉnh sửa:

```csharp
// Config vị trí mặc định (Vĩnh Long)
private const double LAT = 10.0668;   // Vĩ độ
private const double LON = 105.9088;  // Kinh độ
```

### Thay đổi thời gian chuyển ảnh

Trong `MainWindow.axaml.cs`:

```csharp
// Setup Slideshow (10 giây đổi ảnh)
_slideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
```

### Thay đổi thời gian cập nhật thời tiết

```csharp
// Timer update thời tiết mỗi 30 phút
var weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
```

## 📁 Cấu trúc thư mục

```
/opt/piclock/
├── PiClock                    # File thực thi
├── PiClock.deps.json
├── PiClock.runtimeconfig.json
├── createdump
└── images/                    # Thư mục chứa ảnh slideshow
    ├── photo1.jpg
    ├── photo2.png
    └── ...
```

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

## 🙏 Cảm ơn

- [Avalonia UI](https://avaloniaui.net/) - Framework UI cross-platform
- [Open-Meteo](https://open-meteo.com/) - API thời tiết miễn phí
- [ImageSharp](https://sixlabors.com/products/imagesharp/) - Thư viện xử lý ảnh

---

<p align="center">Made with ❤️ for Raspberry Pi</p>
