using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nava.Common;
using Nava.Common.Exceptions;
using Nava.Data.Contracts;
using Nava.Entities.Media;
using Nava.Entities.User;
using Nava.Presentation.Models;
using Nava.Services.Services;
using Nava.WebFramework.Api;
using Nava.WebFramework.Filters;
using Role = Nava.Common.Role;

namespace Nava.Presentation.Controllers.v1
{
    [ApiVersion("1")]
    public class UserController : BaseController
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly string _userAvatarPath;
        private readonly IFileRepository _fileRepository;
        private readonly RoleManager<Entities.User.Role> _roleManager;

        public UserController(IUserRepository userRepository,
            UserManager<User> userManager,
            IJwtService jwtService, IMapper mapper,
            IFileRepository fileRepository,
            RoleManager<Entities.User.Role> roleManager)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _jwtService = jwtService;
            _mapper = mapper;
            _userAvatarPath = "wwwroot\\user_avatars";
            _fileRepository = fileRepository;
            _roleManager = roleManager;
        }

        [HttpGet]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public virtual async Task<ActionResult<List<UserResultDto>>> Get(CancellationToken cancellationToken)
        {
            var list = await _userRepository.TableNoTracking
                .ProjectTo<UserResultDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public virtual async Task<ActionResult<UserResultDto>> Get(int id, CancellationToken cancellationToken)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.GetByUsernameAsync(authorizedUserName, cancellationToken);

            if (authorizedUser is null) return Unauthorized();
            
            if (authorizedUser.Id != id)
                if (!User.IsInRole(Role.Admin))
                    return Forbid();

            var user = await _userRepository.TableNoTracking.ProjectTo<UserResultDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(u => u.Id.Equals(id), cancellationToken);

            if (user is null)
                throw new NotFoundException($"کاربری با شناسه {id} پیدا نشد.");

            return Ok(user);
        }

        [HttpPost]
        [AllowAnonymous]
        public virtual async Task<ApiResult<UserResultDto>> Create([FromForm] UserDto userDto, CancellationToken cancellationToken)
        {
            userDto.Id = 0;
            var user = userDto.ToEntity(_mapper);

            if (userDto.AvatarFile != null)
                user.AvatarPath = userDto.AvatarFile != null
                    ? _fileRepository.SaveFileAsync(userDto.AvatarFile, _userAvatarPath).GetAwaiter().GetResult()
                        .FileName
                    : null;

            await _userManager.CreateAsync(user, userDto.Password);

            if (await _roleManager.FindByNameAsync(Role.User) == null)
            {
                await _roleManager.CreateAsync(new Entities.User.Role { Name = Role.User });
                await _roleManager.FindByNameAsync(Role.User);
            }
            await _userManager.AddToRoleAsync(user, Role.User);

            var resultDto = await _userRepository.TableNoTracking.ProjectTo<UserResultDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(p => p.Id.Equals(user.Id), cancellationToken);

            return Ok(resultDto);
        }

        /// <summary>
        /// This method generate JWT Token
        /// </summary>
        /// <param name="tokenRequest">The information of token request</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [AllowAnonymous]
        public virtual async Task<ActionResult> Token([FromForm] TokenRequest tokenRequest, CancellationToken cancellationToken)
        {
            if (!tokenRequest.grant_type.Equals("password", StringComparison.OrdinalIgnoreCase))
                return BadRequest("OAuth flow is not password.");

            var user = await _userManager.FindByNameAsync(tokenRequest.username);
            if (user == null)
                return BadRequest("نام کاربری یا رمز عبور اشتباه است");

            if (!user.IsActive)
                return Forbid();

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, tokenRequest.password);
            if (!isPasswordValid)
                return BadRequest("نام کاربری یا رمز عبور اشتباه است");

            var jwt = await _jwtService.GenerateAsync(user);
            return new JsonResult(jwt);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public virtual async Task<ApiResult> Delete(int id, CancellationToken cancellationToken)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.GetByUsernameAsync(authorizedUserName, cancellationToken);

            if (authorizedUser is null) return Unauthorized();

            if (authorizedUser.Id != id)
                if (!User.IsInRole(Role.Admin))
                    return Forbid();

            var user = await _userRepository.Table
                .Include(a => a.LikedMedias)
                .Include(a => a.FollowingArtists)
                .Include(a => a.VisitedMedias)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (user is null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, Role.Admin))
                throw new LogicException("حذف کاربر مدیر امکان پذیر نیست!");

            var rolesForUser = await _userManager.GetRolesAsync(user);

            if (rolesForUser.Any())
                foreach (var role in rolesForUser.ToList())
                    await _userManager.RemoveFromRoleAsync(user, role);

            user.LikedMedias = null;
            user.VisitedMedias = null;
            user.FollowingArtists = null;
            await _userManager.UpdateAsync(user);

            await _userRepository.DeleteAsync(user, cancellationToken);
            _fileRepository.DeleteFile(Path.Combine(_userAvatarPath, user.AvatarPath ?? ""));

            return Ok();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public virtual async Task<ApiResult<UserResultDto>> Update(int id, [FromForm] UserUpdateDto dto, CancellationToken cancellationToken)
        {
            if (dto.Id != id)
                return BadRequest();

            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.GetByUsernameAsync(authorizedUserName, cancellationToken);

            if (authorizedUser is null) return Unauthorized();

            if (authorizedUser.Id != id)
                if (!User.IsInRole(Role.Admin))
                    return Forbid();

            var user = await _userRepository.GetByIdAsync(cancellationToken, id);

            if (dto.NewPassword != null && dto.CurrentPassword != null)
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
                if (!changePasswordResult.Succeeded)
                    return BadRequest("رمز عبور فعلی اشتباه است");
            }

            user = dto.ToEntity(_mapper, user);

            if (dto.AvatarFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(_userAvatarPath, user.AvatarPath ?? ""));

                user.AvatarPath = dto.AvatarFile != null
                    ? _fileRepository.SaveFileAsync(dto.AvatarFile, _userAvatarPath).GetAwaiter().GetResult()
                        .FileName
                    : null;
            }
            await _userManager.UpdateAsync(user);

            var resultDto = await _userRepository.TableNoTracking.ProjectTo<UserResultDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(p => p.Id.Equals(user.Id), cancellationToken);

            return resultDto;
        }

        /// <summary>
        /// Deactivate a user's account by it's unique userId
        /// </summary>
        /// <param name="userId">User's unique Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(DeactivateUserAccount) + "/{userId}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public virtual async Task<ApiResult> DeactivateUserAccount(int userId, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(cancellationToken, userId);

            if (user is null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, Role.Admin))
                return BadRequest("کاربر مدیر نمی تواند غیرفعال شود!");
            
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            return Ok();
        }
    }
}
