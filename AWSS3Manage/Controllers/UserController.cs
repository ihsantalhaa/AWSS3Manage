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
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IAmazonS3 _s3Client;

        public UserController(ApplicationDbContext db, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IConfiguration configuration, IAmazonS3 s3Client)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _s3Client = s3Client;
        }

        [HttpPost]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> UserDeleteRole(RoleUserViewModel model)
        {
            AppUser user = await _userManager.FindByIdAsync(model.UserId.ToString());
            AppRole role = await _roleManager.FindByIdAsync(model.RoleId.ToString());
            if (role != null && user != null)
            {
                var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
                if (result.Succeeded)
                {
                    return RedirectToAction("UserListRole", "User", new { id = user.Id });
                }
            }
            return NotFound("User Not Found In This Role");
        }
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> UserListRole(int? id)
        {
            AppUser findedUser = await _userManager.FindByIdAsync(id.ToString());
            if (findedUser == null)
            {
                return NotFound();
            }
            ViewBag.User = findedUser.UserName.ToString() + "'s roles";
            IList<string> userRoleNameList = await _userManager.GetRolesAsync(findedUser);
            List<RoleUserViewModel> modelList = new List<RoleUserViewModel>();
            foreach (string roleName in userRoleNameList)
            {
                AppRole findedRole = await _roleManager.FindByNameAsync(roleName);
                RoleUserViewModel model = new RoleUserViewModel
                {
                    UserId = findedUser.Id,
                    UserName = findedUser.UserName,
                    RoleId = findedRole.Id,
                    RoleName = findedRole.Name,
                    RoleDescription = findedRole.RoleDescription,
                };
                modelList.Add(model);
            }
            return View(modelList);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser? findedUser = await _userManager.FindByNameAsync(User?.Identity?.Name);
                if (findedUser == null)
                {
                    return NotFound();
                }
                var result = await _userManager.ChangePasswordAsync(findedUser, model.Password, model.NewPassword); //await _userManager.ResetPasswordAsync(findedUser, model.Token, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("LogOut", "Home");
                }
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
            }
            return View();
        }
        [Authorize]
        public async Task<IActionResult> UpdatePassword()
        {
            AppUser? findedUser = await _userManager.FindByNameAsync(User?.Identity?.Name);
            if (findedUser == null)
            {
                return NotFound("User Not Found!");
            }
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(UserDetailsViewModel? model)
        {
            AppUser? findedUser = await _userManager.FindByNameAsync(User.Identity?.Name);
            if (findedUser == null)
            {
                return NotFound("User Not Found!");
            }
            AppUser? nameFindedUser = await _userManager.FindByNameAsync(model?.Username);
            AppUser? emailFindedUser = await _userManager.FindByEmailAsync(model?.Email);
            if (nameFindedUser != null && findedUser.Id != nameFindedUser.Id)
            {
                return BadRequest("Username Must Be Unique!");
            }

            if (emailFindedUser != null && findedUser.Id != emailFindedUser.Id)
            {
                return BadRequest("Email Must Be Unique!");
            }

            if (ModelState.IsValid)
            {
                if (findedUser.UserName != model?.Username)
                {
                    var bucketName = _configuration["AWS:S3BucketName"];
                    var oldFolderName = findedUser.UserName;
                    var newFolderName = model?.Username;
                    ListObjectsV2Request listRequest = new ListObjectsV2Request
                    {
                        BucketName = bucketName,
                        Prefix = oldFolderName
                    };

                    ListObjectsV2Response listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                    foreach (S3Object entry in listResponse.S3Objects)
                    {
                        string newKey = entry.Key.Replace(oldFolderName, newFolderName);

                        CopyObjectRequest copyRequest = new CopyObjectRequest
                        {
                            SourceBucket = bucketName,
                            SourceKey = entry.Key,
                            DestinationBucket = bucketName,
                            DestinationKey = newKey
                        };

                        CopyObjectResponse copyResponse = await _s3Client.CopyObjectAsync(copyRequest);

                        if (copyResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
                        {
                            DeleteObjectRequest deleteRequest = new DeleteObjectRequest
                            {
                                BucketName = bucketName,
                                Key = entry.Key
                            };
                            await _s3Client.DeleteObjectAsync(deleteRequest);
                        }
                        else
                        {
                            return BadRequest("An error occurred while renaming the folder in S3 bucket, try again later.");
                        }
                    }
                }

                findedUser.UserName = model?.Username;
                findedUser.Email = model?.Email;
                var result = await _userManager.UpdateAsync(findedUser);
                if (result.Succeeded)
                {
                    return RedirectToAction("LogOut", "Home");
                }
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
            }
            return View(model);
        }
        [Authorize]
        public async Task<IActionResult> UpdateProfile()
        {
            AppUser userDetails = await _userManager.FindByNameAsync(User.Identity?.Name);
            if (userDetails != null)
            {
                var model = new UserDetailsViewModel();
                model.Username = userDetails.UserName;
                model.Email = userDetails.Email;
                return View(model);
            }
            else
            {
                return NotFound("User Not Found!");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> Delete(int? id)
        {
            var findedUser = await _userManager.FindByIdAsync(id.ToString());
            if (findedUser == null)
            {
                return NotFound("User Not Found!");
            }

            var userFiles = _db.S3files
                .Where(f => f.Id == findedUser.Id)
                .Select(f => f);

            if (userFiles.Any())
            {
                string _bucketName = _configuration["AWS:S3BucketName"];
                string _folderName = findedUser.UserName;
                var key = _folderName + "/";

                ListObjectsV2Request listObjectsRequest = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = _folderName,
                };
                try
                {
                    var listObjectsResponse = await _s3Client.ListObjectsV2Async(listObjectsRequest);
                    foreach (var obj in listObjectsResponse.S3Objects)
                    {
                        var deleteObjectRequest = new DeleteObjectRequest
                        {
                            BucketName = _bucketName,
                            Key = obj.Key
                        };
                        await _s3Client.DeleteObjectAsync(deleteObjectRequest);
                    }
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
            }

            var result = await _userManager.DeleteAsync(findedUser);
            if (result.Succeeded)
            {
                return RedirectToAction("ListUsers");
            }
            else
            {
                return BadRequest("User Not Removed");
            }
        }

        [Authorize(Roles = "Root")]
        public async Task<IActionResult> DeleteUser(int? id)
        {
            var findedUser = await _userManager.FindByIdAsync(id.ToString());
            if (findedUser != null)
            {
                var model = new UserDetailsViewModel();
                model.Id = findedUser.Id;
                model.Username = findedUser.UserName;
                model.Email = findedUser.Email;
                return View(model);
            }
            else
            {
                return NotFound("User Not Found!");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> UpdateUser(UserDetailsViewModel model)
        {
            AppUser? findedUser = await _userManager.FindByIdAsync(model.Id.ToString());
            if (findedUser == null)
            {
                return NotFound("User Not Found!");
            }

            if (ModelState.IsValid)
            {
                findedUser.UserDescription = model.UserDescription;
                var result = await _userManager.UpdateAsync(findedUser);
                if (result.Succeeded)
                {
                    return RedirectToAction("ListUsers");
                }
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
            }
            return View(model);
        }

        [Authorize(Roles = "Root")]
        public async Task<IActionResult> UpdateUser(int? id)
        {
            var userDetails = await _userManager.FindByIdAsync(id.ToString());
            if (userDetails != null)
            {
                var model = new UserDetailsViewModel();
                model.Id = userDetails.Id;
                model.Username = userDetails.UserName;
                model.Email = userDetails.Email;
                model.UserDescription = userDetails.UserDescription;
                return View(model);
            }
            else
            {
                return NotFound("User Not Found!");
            }
        }

        [Authorize(Roles = "Root")]
        public IActionResult ListUsers()
        {
            var users = _userManager.Users.ToList();
            List<UserDetailsViewModel> modelList = new List<UserDetailsViewModel>();
            foreach (var i in users)
            {
                UserDetailsViewModel item = new UserDetailsViewModel
                {
                    Id = i.Id,
                    Username = i.UserName,
                    Email = i.Email,
                    UserDescription = i.UserDescription,
                };
                modelList.Add(item);
            }
            return View(modelList);
        }
    }
}
