namespace Ideku.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string?> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        public void DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}