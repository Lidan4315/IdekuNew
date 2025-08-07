// Services/FileService.cs (Updated for new ID format)
namespace Ideku.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string?> SaveFileAsync(IFormFile file, string ideaId, int currentStage, int fileIndex)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var stageString = $"S{currentStage}";
            var fileExtension = Path.GetExtension(file.FileName);
            var tempFileIndex = fileIndex;
            var fileName = $"{ideaId}_{stageString}_{tempFileIndex:D3}{fileExtension}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Handle potential file name conflicts by incrementing the index
            while (File.Exists(filePath))
            {
                tempFileIndex++;
                fileName = $"{ideaId}_{stageString}_{tempFileIndex:D3}{fileExtension}";
                filePath = Path.Combine(uploadPath, fileName);
            }

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