using Amazon.S3;
using Amazon.S3.Model;
using AWSS3Manage.Data;
using AWSS3Manage.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace AWSS3Manage.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IAmazonS3 _s3Client;

        public FileController(ApplicationDbContext db, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IConfiguration configuration, IAmazonS3 s3Client)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _s3Client = s3Client;
        }

        [HttpPost]
        public async Task<IActionResult> DownloadFile(int? fileId)
        {
            S3file? file = await _db.S3files.FindAsync(fileId);
            AppUser? fileUser = await _userManager.FindByIdAsync(file?.Id.ToString());
            AppUser? authUser = await _userManager.FindByNameAsync(User?.Identity?.Name);

            if (fileUser == null || file == null)
            {
                return NotFound("File or user not found");
            }

            if (fileUser != authUser)
            {
                return BadRequest("You don't own this file");
            }

            string _bucketName = _configuration["AWS:S3BucketName"];
            string _folderName = fileUser.UserName;
            var key = _folderName + "/" + file.FileName;

            var response = await _s3Client.GetObjectAsync(_bucketName, key);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return File(response.ResponseStream, response.Headers.ContentType, key);
            }
            else
            {
                return BadRequest("Error While Downloading The File");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int? fileId)
        {
            S3file? file = await _db.S3files.FindAsync(fileId);
            AppUser? fileUser = await _userManager.FindByIdAsync(file?.Id.ToString());
            AppUser? authUser = await _userManager.FindByNameAsync(User?.Identity?.Name);
            if (fileUser == null || file == null)
            {
                return NotFound("File or user not found");
            }

            if (fileUser != authUser)
            {
                return BadRequest("You don't own this file");
            }

            string _bucketName = _configuration["AWS:S3BucketName"];
            string _folderName = authUser.UserName;
            var key = _folderName + "/" + file.FileName;

            DeleteObjectRequest deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
            };
            await _s3Client.DeleteObjectAsync(deleteRequest);

            _db.S3files.Remove(file);
            await _db.SaveChangesAsync();
            return RedirectToAction("UserFilesView", "File");
        }

        public async Task<IActionResult> DeleteFile(int? id)
        {
            S3file? file = await _db.S3files.FindAsync(id);
            if (file == null)
            {
                return NotFound("File Not Found!");
            }

            FileDetailsViewModel model = new FileDetailsViewModel
            {
                FileId = file.FileId,
                FileName = file!.FileName!,
                FileDescription = file.FileDescription,
                UploadDate = file.UploadDate,
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditFile(FileDetailsViewModel? model)
        {
            S3file? file = await _db.S3files.FindAsync(model?.FileId);
            AppUser? fileUser = await _userManager.FindByIdAsync(file?.Id.ToString());
            AppUser? authUser = await _userManager.FindByNameAsync(User?.Identity?.Name);
            if (fileUser == null || file == null)
            {
                return NotFound("File or user not found");
            }

            if (fileUser != authUser)
            {
                return BadRequest("You don't own this file");
            }

            var findedOtherFile = from f in _db.S3files
                                  where f.FileName == model!.FileName && f.Id == fileUser.Id //This id is user id, look at model
                                  select f;

            S3file? otherFile = findedOtherFile.FirstOrDefault();
            if (otherFile != null && otherFile.FileId != file.FileId)
            {
                return BadRequest("A file already exists with the same file name.");
            }

            string _bucketName = _configuration["AWS:S3BucketName"];
            string _folderName = authUser.UserName;
            var key = _folderName + "/" + file.FileName;
            var newKey = _folderName + "/" + model!.FileName;

            if (file.FileName != model!.FileName || file.FileDescription != model.FileDescription)
            {
                if (file.FileName != model!.FileName)
                {

                    CopyObjectRequest copyRequest = new CopyObjectRequest
                    {
                        SourceBucket = _bucketName,
                        DestinationBucket = _bucketName,
                        SourceKey = key,
                        DestinationKey = newKey,
                    };
                    CopyObjectResponse copyResponse = await _s3Client.CopyObjectAsync(copyRequest);

                    if (copyResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        DeleteObjectRequest deleteRequest = new DeleteObjectRequest
                        {
                            BucketName = _bucketName,
                            Key = key,
                        };
                        await _s3Client.DeleteObjectAsync(deleteRequest);
                        file.FileName = model!.FileName;
                    }
                    else
                    {
                        return BadRequest("Error While Renaming The File");
                    }
                }
                file.FileDescription = model!.FileDescription;
                _db.S3files.Update(file!);
                _db.SaveChanges();

            }
            else
            {
                return BadRequest("Nothing has changed");
            }
            return RedirectToAction("UserFilesView", "File");
        }

        public async Task<IActionResult> EditFile(int? id)
        {
            S3file? file = await _db.S3files.FindAsync(id);
            if (file == null)
            {
                return BadRequest("File not Found!");
            }

            FileDetailsViewModel model = new FileDetailsViewModel
            {
                FileId = file.FileId,
                FileName = file!.FileName!,
                FileDescription = file.FileDescription,
            };
            return View(model);
        }

        [HttpPost]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> UploadFile(IFormFile file, IFormCollection data)
        {
            AppUser? user = await _userManager.FindByNameAsync(User?.Identity?.Name);
            if (user == null)
            {
                return BadRequest("User Not Found!");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("Please select a file.");
            }

            var findedOtherFile = from f in _db.S3files
                                  where f.FileName == file.FileName && f.Id == user.Id
                                  select f;

            S3file? otherFile = findedOtherFile.FirstOrDefault();
            if (otherFile != null)
            {
                return BadRequest("A file already exists with the same file name.");
            }

            string fileSize = FormatFileSize(file.Length);

            string _bucketName = _configuration["AWS:S3BucketName"];
            string _folderName = user.UserName;
            var key = _folderName + "/" + file.FileName;

            string fileDescription = data["txtFileDescription"].ToString();

            InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            InitiateMultipartUploadResponse initiateResponse = await _s3Client.InitiateMultipartUploadAsync(initiateRequest);

            List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();
            using (var fileStream = file.OpenReadStream())
            {
                long partSize = 5 * 1024 * 1024; // Parça boyutu (5 MB)

                for (int partNumber = 1; ; partNumber++)
                {
                    byte[] buffer = new byte[partSize];
                    int bytesRead = fileStream.Read(buffer, 0, (int)partSize);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    UploadPartRequest uploadRequest = new UploadPartRequest
                    {
                        BucketName = _bucketName,
                        Key = key,
                        UploadId = initiateResponse.UploadId,
                        PartNumber = partNumber,
                        InputStream = new MemoryStream(buffer, 0, bytesRead)
                    };

                    UploadPartResponse uploadResponse = await _s3Client.UploadPartAsync(uploadRequest);
                    uploadResponses.Add(uploadResponse);

                    //int progress = partNumber; //* 100 / (totalFileSize / partSize);
                    //string message = "Upload status : %" + progress.ToString();

                }
            }

            CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = key,
                UploadId = initiateResponse.UploadId,
                PartETags = uploadResponses.Select(r => new PartETag { PartNumber = r.PartNumber, ETag = r.ETag }).ToList()
            };

            CompleteMultipartUploadResponse completeResponse = await _s3Client.CompleteMultipartUploadAsync(completeRequest);


            S3file s3file = new S3file
            {
                FileName = file.FileName,
                FileDescription = fileDescription,
                FileSize = fileSize,
                UploadDate = DateTime.Now,
                Id = user.Id,
                AppUser = user,
            };

            await _db.S3files.AddAsync(s3file);
            await _db.SaveChangesAsync();
            return RedirectToAction("UserFilesView");
        }

        public IActionResult UploadFile()
        {
            return View();
        }

        public async Task<IActionResult> UserFilesView()
        {
            AppUser? user = await _userManager.FindByNameAsync(User.Identity?.Name);
            if (user == null)
            {
                return NotFound("User Not Found!");
            }
            ViewBag.User = user.UserName + "'s files";
            var userFiles = from u in _db.S3files
                            where u.Id == user.Id
                            select u;
            List<FileDetailsViewModel>? modelList = userFiles?.Select(file => new FileDetailsViewModel
            {
                FileId = file.FileId,
                FileName = file.FileName,
                FileSize = file.FileSize,
                FileDescription = file.FileDescription,
                UploadDate = file.UploadDate
            }).ToList();
            return View(modelList);
        }

        static string FormatFileSize(long fileSizeInBytes)
        {
            string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = fileSizeInBytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
