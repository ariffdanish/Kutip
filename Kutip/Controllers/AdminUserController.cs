using Kutip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class AdminUserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminUserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();
        var userWithRoles = new List<(ApplicationUser user, IList<string> roles)>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userWithRoles.Add((user, roles));
        }

        return View(userWithRoles);
    }

    public IActionResult Create()
    {
        var model = new CreateUserViewModel
        {
            Role = "TruckDriver"
        };

        ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.Role) && await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                TempData["Success"] = "User created successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name));
        return View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var model = new CreateUserViewModel
        {
            Email = user.Email,
            Role = currentRoles.FirstOrDefault()
        };

        ViewBag.UserId = user.Id;
        ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CreateUserViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (ModelState.IsValid)
        {
            user.Email = model.Email;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            // Change password if provided
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                // Remove old password (if exists) and add new one
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(model);
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrEmpty(model.Role) && await _roleManager.RoleExistsAsync(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            TempData["Success"] = "User updated successfully!";
            return RedirectToAction("Index");
        }

        ViewBag.UserId = user.Id;
        ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name));
        return View(model);
    }


    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "User deleted successfully!";
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View("Delete", user);
    }
}
