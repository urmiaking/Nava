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
        public async Task<ActionResult<List<UserResultDto>>> Get(CancellationToken cancellationToken)
        {
            var list = await _userRepository.TableNoTracking
                .ProjectTo<UserResultDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<User>> Get(int id, CancellationToken cancellationToken)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.GetByUsernameAsync(authorizedUserName, cancellationToken);

            if (authorizedUser is null) throw new UnauthorizedAccessException();

            if (authorizedUser.Id != id)
                if (!User.IsInRole(Role.Admin))
                    throw new UnauthorizedAccessException();

            var user = await _userRepository.TableNoTracking.ProjectTo<UserResultDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(u => u.Id.Equals(id), cancellationToken);

            if (user is null)
                throw new NotFoundException($"کاربری با شناسه {id} پیدا نشد.");

            return Ok(user);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ApiResult<UserResultDto>> Create([FromForm] UserDto userDto, CancellationToken cancellationToken)
        {
            userDto.Id = 0;
            var user = userDto.ToEntity(_mapper);

            if (userDto.AvatarFile != null)
            {
                var avatarSaveResult = await _fileRepository.SaveFileAsync(userDto.AvatarFile, _userAvatarPath);

                user.AvatarPath = avatarSaveResult.FileCreationStatus switch
                {
                    FileCreationStatus.Success => avatarSaveResult.FileName,
                    FileCreationStatus.Failed => throw new BadRequestException("خطا در ثبت نام"),
                    _ => null
                };
            }

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
                throw new Exception("OAuth flow is not password.");

            //var user = await userRepository.GetByUserAndPass(username, password, cancellationToken);
            var user = await _userManager.FindByNameAsync(tokenRequest.username);
            if (user == null)
                throw new BadRequestException("نام کاربری یا رمز عبور اشتباه است");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, tokenRequest.password);
            if (!isPasswordValid)
                throw new BadRequestException("نام کاربری یا رمز عبور اشتباه است");


            //if (user == null)
            //    throw new BadRequestException("نام کاربری یا رمز عبور اشتباه است");

            var jwt = await _jwtService.GenerateAsync(user);
            return new JsonResult(jwt);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Delete(int id, CancellationToken cancellationToken)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.GetByUsernameAsync(authorizedUserName, cancellationToken);

            if (authorizedUser is null) throw new UnauthorizedAccessException();

            if (authorizedUser.Id != id)
                if(!User.IsInRole(Role.Admin))
                    throw new UnauthorizedAccessException("Restrict access.");

            var user = await _userRepository.GetByIdAsync(cancellationToken, id);

            if (user is null)
                throw new NotFoundException($"کاربری با شناسه {id} پیدا نشد.");

            var rolesForUser = await _userManager.GetRolesAsync(user);

            if (rolesForUser.Any())
                foreach (var role in rolesForUser.ToList())
                    await _userManager.RemoveFromRoleAsync(user, role);
            
            await _userRepository.DeleteAsync(user, cancellationToken);
            _fileRepository.DeleteFile(Path.Combine(_userAvatarPath, user.AvatarPath ?? ""));

            return Ok();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<UserResultDto>> Update(int id, [FromForm] UserUpdateDto dto, CancellationToken cancellationToken)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.GetByUsernameAsync(authorizedUserName, cancellationToken);

            if (authorizedUser is null) throw new UnauthorizedAccessException();

            if (authorizedUser.Id != id)
                if (!User.IsInRole(Role.Admin))
                    throw new UnauthorizedAccessException("Restrict access.");

            dto.Id = id;
            
            var user = await _userRepository.GetByIdAsync(cancellationToken, id);

            user = dto.ToEntity(_mapper, user);

            if (dto.AvatarFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(_userAvatarPath, user.AvatarPath ?? ""));
                var avatarSaveResult = await _fileRepository.SaveFileAsync(dto.AvatarFile, _userAvatarPath);

                user.AvatarPath = avatarSaveResult.FileCreationStatus switch
                {
                    FileCreationStatus.Success => avatarSaveResult.FileName,
                    FileCreationStatus.Failed => throw new BadRequestException("درج تصویر جدید با مشکل مواجه شد"),
                    _ => null
                };
            }

            await _userManager.UpdateAsync(user);

            if (dto.NewPassword != null && dto.CurrentPassword != null)
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
                if (!changePasswordResult.Succeeded)
                    throw new BadRequestException("رمز عبور فعلی اشتباه است");
            }

            var resultDto = await _userRepository.TableNoTracking.ProjectTo<UserResultDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(p => p.Id.Equals(user.Id), cancellationToken);

            return resultDto;
        }
    }
}
