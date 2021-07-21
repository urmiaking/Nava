using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

namespace Nava.Presentation.Controllers.v2
{
    [ApiVersion("2")]
    public class VisitsController : BaseController
    {
        private readonly IMongoRepository<User> _userRepository;
        private readonly IMongoRepository<Media> _mediaRepository;
        private readonly IMapper _mapper;

        public VisitsController(IMongoRepository<User> userRepository, IMongoRepository<Media> mediaRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mediaRepository = mediaRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// A method for visiting a media by it's unique Id for authenticated user
        /// </summary>
        /// <param name="id">User's Unique id</param>
        /// <returns></returns>
        [HttpGet(nameof(Visit) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Visit(string id)
        {
            var media = await _mediaRepository.FindByIdAsync(id);

            if (media is null)
                return NotFound();

            var username = User.Identity?.Name;
            var visitedUser = await _userRepository.FindOneAsync(a => a.UserName.Equals(username));

            if (visitedUser is null) return Unauthorized();

            if (visitedUser.VisitedMedias.Exists(a => a.Equals(media.Id)))
                return Ok();
            
            visitedUser.VisitedMedias.Add(media.Id);
            media.VisitedUsers.Add(visitedUser.Id);

            await _mediaRepository.ReplaceOneAsync(media);
            await _userRepository.ReplaceOneAsync(visitedUser);

            return Ok();
        }
        
        /// <summary>
        /// Get Medias which has visited by a user with it's unique Id
        /// </summary>
        /// <param name="id">User's unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetVisitedMedias) + "/{id}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<MongoMediaResultDto>>> GetVisitedMedias(string id)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.FindOneAsync(a => a.UserName.Equals(authorizedUserName));

            if (authorizedUser is null) return Unauthorized();

            if (authorizedUser.Id != new ObjectId(id))
                if (!User.IsInRole(Role.Admin))
                    return Forbid();

            var user = await _userRepository.FindByIdAsync(id);

            if (user is null)
                return NotFound();

            var visitedMedias = new List<Media>();

            foreach (var visitedMediaId in user.VisitedMedias)
                visitedMedias.Add(await _mediaRepository.FindByIdAsync(visitedMediaId.ToString()));
            
            var mediaDtoList = new List<MongoMediaResultDto>(visitedMedias.Capacity);
            mediaDtoList.AddRange(visitedMedias.Select(media => MongoMediaResultDto.FromEntity(_mapper, media)));

            return Ok(mediaDtoList);
        }
        
        /// <summary>
        /// Get Users which visited a media by it's unique Id
        /// </summary>
        /// <param name="id">>Media's unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetVisitedUsers) + "/{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<MongoUserResultDto>>> GetVisitedUsers(string id)
        {
            var media = await _mediaRepository.FindByIdAsync(id);

            if (media is null)
                return NotFound();

            var visitedUsers = new List<User>();

            foreach (var visitedUserId in media.VisitedUsers)
                visitedUsers.Add(await _userRepository.FindByIdAsync(visitedUserId.ToString()));
            
            var userDtoList = new List<MongoUserResultDto>(visitedUsers.Capacity);
            userDtoList.AddRange(visitedUsers.Select(user => MongoUserResultDto.FromEntity(_mapper, user)));

            return Ok(userDtoList);
        }
    }
}
