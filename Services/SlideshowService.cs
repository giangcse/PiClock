using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using PiClock.Models;
using AvaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace PiClock.Services;

public class SlideshowService
{
    private string[] _imageFiles = Array.Empty<string>();
    private int _currentIndex = 0;
    private readonly string _imageFolder;

    public int ImageCount => _imageFiles.Length;
    public bool HasImages => _imageFiles.Length > 0;

    public SlideshowService(SlideshowConfig config)
    {
        _imageFolder = Path.Combine(AppContext.BaseDirectory, config.ImageFolder);
    }

    public void LoadImages()
    {
        try
        {
            if (!Directory.Exists(_imageFolder))
            {
                Directory.CreateDirectory(_imageFolder);
                Console.WriteLine($"📁 Đã tạo thư mục ảnh: {_imageFolder}");
            }

            var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            _imageFiles = Directory.GetFiles(_imageFolder)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            if (_imageFiles.Length > 0)
            {
                Console.WriteLine($"🖼️ Đã tải {_imageFiles.Length} ảnh");
            }
            else
            {
                Console.WriteLine("⚠️ Thư mục images trống");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi quét thư mục ảnh: {ex.Message}");
        }
    }

    public async Task<AvaBitmap?> GetNextImageAsync()
    {
        if (_imageFiles.Length == 0) return null;

        string currentFile = _imageFiles[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _imageFiles.Length;

        try
        {
            return await Task.Run(() =>
            {
                using var image = SixLabors.ImageSharp.Image.Load(currentFile);
                image.Mutate(x => x.AutoOrient());
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(1920, 1080),
                    Mode = ResizeMode.Max
                }));

                var memoryStream = new MemoryStream();
                image.SaveAsBmp(memoryStream);
                memoryStream.Position = 0;
                return new AvaBitmap(memoryStream);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi load ảnh: {ex.Message}");
            return null;
        }
    }

    public void Reset()
    {
        _currentIndex = 0;
    }
}
