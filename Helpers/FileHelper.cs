using Microsoft.AspNetCore.Http;
using System.IO;

namespace QLTTYKPH.Helpers
{
    public static class FileHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
        private const int MaxFileSizeMB = 5;

        public static async Task<string?> UploadFileAsync(IFormFile? file, string folderName, string webRootPath)
        {
            if (file == null || file.Length == 0)
                return null;

            // Kiểm tra dung lượng
            if (file.Length > MaxFileSizeMB * 1024 * 1024)
                throw new Exception($"File tải lên không được vượt quá {MaxFileSizeMB}MB.");

            // Kiểm tra định dạng
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(ext))
                throw new Exception("Định dạng file không được hỗ trợ. Vui lòng tải lên ảnh (JPG, PNG) hoặc tài liệu (PDF, DOCX, XLSX).");

            var uploadsFolder = Path.Combine(webRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Trả về đường dẫn tương đối để lưu vào DB
            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public static async Task<string?> UploadMultipleFilesAsync(List<IFormFile>? files, string folderName, string webRootPath)
        {
            if (files == null || files.Count == 0)
                return null;

            var paths = new List<string>();
            foreach (var file in files)
            {
                var path = await UploadFileAsync(file, folderName, webRootPath);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            return paths.Count > 0 ? string.Join(";", paths) : null;
        }
    }
}
