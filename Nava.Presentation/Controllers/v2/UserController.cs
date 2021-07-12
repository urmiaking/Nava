using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using Nava.Common;
using Nava.Common.Exceptions;
using Nava.Common.Utilities;
using Nava.Data.Contracts;
using Nava.Entities.Media;
using Nava.Entities.MongoDb;
using Nava.Entities.User;
using Nava.Presentation.Models;
using Nava.Services.Services;
using Nava.WebFramework.Api;
using Role = Nava.Common.Role;

namespace Nava.Presentation.Controllers.v2
{
    [ApiVersion("2")]
    public class UserController : BaseController
    {
        private readonly IMongoRepository<Entities.MongoDb.User> _mongoRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IMapper _mapper;
        private readonly IJwtService _jwtService;
        private const string UserAvatarPath = "wwwroot\\user_avatars";

        public UserController(IMongoRepository<Entities.MongoDb.User> mongoRepository, IFileRepository fileRepository, IMapper mapper, IJwtService jwtService)
        {
            _mongoRepository = mongoRepository;
            _fileRepository = fileRepository;
            _mapper = mapper;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Deactivate a user's account by it's unique userId
        /// </summary>
        /// <param name="userId">User's unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(DeactivateUserAccount) + "/{userId}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> DeactivateUserAccount(string userId)
        {
            var user = await _mongoRepository.FindByIdAsync(userId);
            if (user is null)
                throw new NotFoundException("کاربر یافت نشد");

            user.IsActive = false;
            await _mongoRepository.ReplaceOneAsync(user);
            return Ok();
        }

        /// <summary>
        /// This method generate JWT Token
        /// </summary>
        /// <param name="tokenRequest">The information of token request</param>
        /// <returns></returns>
        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<ActionResult> Token([FromForm] TokenRequest tokenRequest)
        {
            if (!tokenRequest.grant_type.Equals("password", StringComparison.OrdinalIgnoreCase))
                throw new Exception("OAuth flow is not password.");

            var user = await _mongoRepository.FindOneAsync(a => a.UserName.Equals(tokenRequest.username));
            if (user == null)
                throw new BadRequestException("نام کاربری یا رمز عبور اشتباه است");

            if (!user.IsActive)
                return new ForbidResult();

            var isPasswordValid = user.PasswordHash.Equals(SecurityHelper.GetSha256Hash(tokenRequest.password));
            if (!isPasswordValid)
                throw new BadRequestException("نام کاربری یا رمز عبور اشتباه است");

            user.SecurityStamp = Guid.NewGuid().ToString();
            await _mongoRepository.ReplaceOneAsync(user);

            var jwt = _jwtService.GenerateForMongo(user);
            return new JsonResult(jwt);
        }

        [HttpGet]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public ActionResult<List<MongoUserResultDto>> Get()
        {
            var users = _mongoRepository.AsQueryable().ToList();
            var userResultList = _mapper.Map<List<Entities.MongoDb.User>, List<MongoUserResultDto>>(users);
            return Ok(userResultList);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<MongoUserResultDto>> Get(string id)
        {
            var user = await _mongoRepository.FindByIdAsync(id);

            if (user is null)
                throw new NotFoundException();

            var userResult = MongoUserResultDto.FromEntity(_mapper, user);
            return Ok(userResult);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ApiResult<MongoUserResultDto>> Create([FromForm] MongoUserDto userDto)
        {
            var user = userDto.ToEntity(_mapper);
            user.AvatarPath = userDto.AvatarFile != null
                ? _fileRepository.SaveFileAsync(userDto.AvatarFile, UserAvatarPath).GetAwaiter().GetResult()
                    .FileName
                : null;
            user.IsActive = true;
            user.Id = ObjectId.GenerateNewId(DateTime.Now);
            user.PasswordHash = SecurityHelper.GetSha256Hash(userDto.Password);
            user.Roles.Add(Role.User);

            #region OldWay
            /*
            var user = new Entities.MongoDb.User
            {
                Id = ObjectId.GenerateNewId(DateTime.Now),
                FullName = userDto.FullName,
                Bio = userDto.Bio,
                PasswordHash = SecurityHelper.GetSha256Hash(userDto.Password),
                IsActive = true,
                UserName = userDto.UserName,
                AvatarPath = userDto.AvatarFile != null
                    ? _fileRepository.SaveFileAsync(userDto.AvatarFile, UserAvatarPath).GetAwaiter().GetResult()
                        .FileName
                    : null
            };*/
            /*var userResult = new MongoUserResultDto
            {
                AvatarPath = user.AvatarPath,
                Bio = user.Bio,
                FullName = user.FullName,
                Id = user.Id.ToString(),
                UserName = user.UserName
            };*/

            #endregion

            await _mongoRepository.InsertOneAsync(user);

            return Ok(MongoUserResultDto.FromEntity(_mapper, user));
        }

        [HttpPut("{id}")]
        public async Task<ApiResult<MongoUserResultDto>> Update(string id, [FromForm] MongoUserUpdateDto dto)
        {
            if (dto.Id != id)
                return BadRequest();

            var user = await _mongoRepository.FindByIdAsync(id);
            user.Bio = dto.Bio;
            user.FullName = dto.FullName;
            user.UserName = dto.UserName;

            if (dto.NewPassword != null && dto.CurrentPassword != null)
                if (user.PasswordHash.Equals(SecurityHelper.GetSha256Hash(dto.CurrentPassword)))
                    user.PasswordHash = SecurityHelper.GetSha256Hash(dto.NewPassword);
                else
                    throw new BadRequestException("رمز عبور فعلی اشتباه است");

            if (dto.AvatarFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(UserAvatarPath, user.AvatarPath ?? ""));
                var avatarSaveResult = await _fileRepository.SaveFileAsync(dto.AvatarFile, UserAvatarPath);

                user.AvatarPath = avatarSaveResult.FileCreationStatus switch
                {
                    FileCreationStatus.Success => avatarSaveResult.FileName,
                    FileCreationStatus.Failed => throw new BadRequestException("درج تصویر جدید با مشکل مواجه شد"),
                    _ => null
                };
            }
            user.SecurityStamp = Guid.NewGuid().ToString();
            await _mongoRepository.ReplaceOneAsync(user);

            return Ok(MongoUserResultDto.FromEntity(_mapper, user));
        }

        [HttpDelete("{id}")]
        public async Task<ApiResult> Delete(string id)
        {
            var user = await _mongoRepository.FindByIdAsync(id);

            if (user is null)
                throw new NotFoundException();
            _fileRepository.DeleteFile(Path.Combine(UserAvatarPath, user.AvatarPath ?? ""));
            await _mongoRepository.DeleteByIdAsync(id);
            return Ok();
        }
    }
}
