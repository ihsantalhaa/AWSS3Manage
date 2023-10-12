using AWSS3Manage.Data;
using AWSS3Manage.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System;
using Microsoft.AspNetCore.Authorization;

namespace AWSS3Manage.Controllers
{
    [Authorize(Roles = "Root")]
    public class RoleController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;

        public RoleController(ApplicationDbContext db, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost]
        public async Task<IActionResult> RoleAddUser(RoleUserViewModel model)
        {
            AppUser? user = await _userManager.FindByIdAsync(model.UserId.ToString());
            AppRole? role = await _roleManager.FindByIdAsync(model.RoleId.ToString());
            if (role != null && user != null)
            {
                var result = await _userManager.AddToRoleAsync(user, role.Name);
                if (result.Succeeded)
                {
                    return RedirectToAction("RoleListUsers", "Role", new { id = role.Id });
                }
            }
            return NotFound("User Already Exist In Role");
        }

        public async Task<IActionResult> RoleAddUser(int? id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound();
            }
            ViewBag.Role = "Add users to " + role.Name;
            ViewData["RoleId"] = role.Id;
            var users = _userManager.Users.ToList();
            List<RoleUserViewModel>? modelList = users.Select(user => new RoleUserViewModel
            {
                RoleId = role.Id,
                UserId = user.Id,
                UserName = user.UserName,
                UserDescription = user.UserDescription
            }).ToList();
            return View(modelList);
        }

        [HttpPost]
        public async Task<IActionResult> RoleDeleteUser(RoleUserViewModel model)
        {
            AppUser user = await _userManager.FindByIdAsync(model.UserId.ToString());
            AppRole role = await _roleManager.FindByIdAsync(model.RoleId.ToString());
            if (role != null && user != null)
            {
                var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
                if (result.Succeeded)
                {
                    return RedirectToAction("RoleListUsers", "Role", new { id = role.Id });
                }
            }
            return NotFound("User Not Found In This Role");
        }

        public async Task<IActionResult> RoleListUsers(int? id)
        {
            AppRole? role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound("Role Not Found!");
            }
            ViewBag.Role = role.Name + "'s users";
            ViewData["RoleId"] = role.Id;
            IList<AppUser>? usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            List<RoleUserViewModel>? modelList = usersInRole.Select(user => new RoleUserViewModel
            {
                RoleId = role.Id,
                UserId = user.Id,
                UserName = user.UserName,
                UserDescription = user.UserDescription
            }).ToList();
            return View(modelList);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int? id)
        {
            AppRole? findedRole = await _roleManager.FindByIdAsync(id.ToString());
            if (findedRole == null)
            {
                return NotFound("Role not found!");
            }
            var result = await _roleManager.DeleteAsync(findedRole);
            if (result.Succeeded)
            {
                return RedirectToAction("ListRoles");
            }
            return View();
        }

        public async Task<IActionResult> DeleteRole(int? id)
        {
            AppRole? findedRole = await _roleManager.FindByIdAsync(id.ToString());
            if (findedRole == null)
            {
                return NotFound("Role not found!");
            }
            RoleDetailsViewModel model = new RoleDetailsViewModel
            {
                Id = findedRole.Id,
                RoleName = findedRole.Name,
                RoleDescription = findedRole.RoleDescription,
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRole(RoleDetailsViewModel model)
        {
            AppRole? findedRole = await _roleManager.FindByIdAsync(model.Id.ToString());
            if (findedRole == null)
            {
                return NotFound();
            }
            AppRole? nameFindedRole = await _roleManager.FindByNameAsync(model.RoleName);
            if (nameFindedRole != null && nameFindedRole.Id != findedRole.Id)
            {
                return BadRequest("Role Name Must Be Unique!");
            }

            if (ModelState.IsValid)
            {
                findedRole.Name = model.RoleName;
                findedRole.RoleDescription = model.RoleDescription;
                var result = await _roleManager.UpdateAsync(findedRole);
                if (result.Succeeded)
                {
                    return RedirectToAction("ListRoles", "Role");
                }
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
            }
            return View(model);
        }
        public async Task<IActionResult> UpdateRole(int? id)
        {
            AppRole? findedRole = await _roleManager.FindByIdAsync(id.ToString());  
            if (findedRole == null)
            {
                return NotFound("Role not found!");
            }
            RoleDetailsViewModel model = new RoleDetailsViewModel
            {
                Id = findedRole.Id,
                RoleName = findedRole.Name,
                RoleDescription = findedRole.RoleDescription,
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(RoleDetailsViewModel model)
        {
            if (ModelState.IsValid)
            {
                AppRole? otherRole = await _roleManager.FindByNameAsync(model.RoleName);
                if (otherRole != null)
                {
                    return BadRequest("Role name must be unique!");
                }

                AppRole role = new AppRole
                {
                    Name = model.RoleName,
                    RoleDescription = model.RoleDescription,
                };
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction("ListRoles", "Role");
                }
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
            }
            return View(model);
        }

        public IActionResult CreateRole()
        {
            return View();
        }

        public IActionResult ListRoles()
        {
            var roles = _roleManager.Roles.ToList();
            List<RoleDetailsViewModel> modelList = new List<RoleDetailsViewModel>();
            foreach (var i in roles)
            {
                RoleDetailsViewModel item = new RoleDetailsViewModel
                {
                    Id = i.Id,
                    RoleName = i.Name,
                    RoleDescription = i.RoleDescription,
                };
                modelList.Add(item);
            }
            return View(modelList);
        }
    }
}
