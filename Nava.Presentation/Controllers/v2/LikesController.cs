using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using Nava.Common;
using Nava.Common.Exceptions;
using Nava.Data.Contracts;
using Nava.Entities.MongoDb;
using Nava.Presentation.Models;
using Nava.WebFramework.Api;
using Media = Nava.Entities.MongoDb.Media;

namespace Nava.Presentation.Controllers.v2
{
    [ApiVersion("2")]
    public class LikesController : BaseController
    {
        private readonly IMongoRepository<User> _userRepository;
        private readonly IMongoRepository<Media> _mediaRepository;
        private readonly IMapper _mapper;

        public LikesController(IMongoRepository<User> userRepository, IMongoRepository<Media> mediaRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mediaRepository = mediaRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Like a media with it's unique Id by an authorized user
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(Like) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Like(string id)
        {
            var media = await _mediaRepository.FindByIdAsync(id);

            if (media is null)
                return NotFound();

            var username = User.Identity?.Name;
            var likedUser = await _userRepository.FindOneAsync(a => a.UserName.Equals(username));

            if (likedUser is null)
                return Unauthorized();

            if (media.LikedUsers.Exists(a => a.Equals(likedUser.Id)))
                return BadRequest("این مدیا قبلا لایک شده است");

            media.LikedUsers.Add(likedUser.Id);
            likedUser.LikedMedias.Add(media.Id);

            await _mediaRepository.ReplaceOneAsync(media);
            await _userRepository.ReplaceOneAsync(likedUser);

            return Ok();
        }

        /// <summary>
        /// Dislike a media with it's unique Id by an authorized user
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(Dislike) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Dislike(string id)
        {
            var media = await _mediaRepository.FindByIdAsync(id);

            if (media is null)
                return NotFound();

            var username = User.Identity?.Name;
            var likedUser = await _userRepository.FindOneAsync(a => a.UserName.Equals(username));

            if (likedUser is null)
                return Unauthorized();

            if (!media.LikedUsers.Exists(a => a.Equals(likedUser.Id)))
                return BadRequest("این مدیا قبلا لایک نشده است");

            media.LikedUsers.Remove(likedUser.Id);
            likedUser.LikedMedias.Remove(media.Id);

            await _mediaRepository.ReplaceOneAsync(media);
            await _userRepository.ReplaceOneAsync(likedUser);

            return Ok();
        }

        /// <summary>
        /// Get medias which has been liked by a user with it's unique Id
        /// </summary>
        /// <param name="id">User's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetLikedMedias) + "/{id}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<MongoMediaResultDto>>> GetLikedMedias(string id)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.FindOneAsync(a => a.UserName.Equals(authorizedUserName));

            if (authorizedUser is null) return Unauthorized();

            if (authorizedUser.Id != new ObjectId(id))
                if (!User.IsInRole(Role.Admin))
                    return Forbid();

            var user = await _userRepository.FindByIdAsync(id);

            if (user is null) return NotFound();

            var likedMediaIds = user.LikedMedias.ToList();

            var likedMedias = new List<Media>();

            foreach (var likedMediaId in likedMediaIds)
                likedMedias.Add(await _mediaRepository.FindByIdAsync(likedMediaId.ToString()));
            
            var mediaDtoList = new List<MongoMediaResultDto>(likedMedias.Capacity);
            mediaDtoList.AddRange(likedMedias.Select(media => MongoMediaResultDto.FromEntity(_mapper, media)));

            return Ok(mediaDtoList);
        }
        
        /// <summary>
        /// Get Users which had liked a media with it's unique Id
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetLikedUsers) + "/{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<MongoUserResultDto>>> GetLikedUsers(string id)
        {
            var media = await _mediaRepository.FindByIdAsync(id);

            if (media is null) return NotFound();

            var likedUsers = new List<User>();

            foreach (var likedUserId in media.LikedUsers)
                likedUsers.Add(await _userRepository.FindByIdAsync(likedUserId.ToString()));

            var userDtoList = new List<MongoUserResultDto>(likedUsers.Capacity);
            userDtoList.AddRange(likedUsers.Select(user => MongoUserResultDto.FromEntity(_mapper, user)));

            return Ok(userDtoList);
        }
    }
}
