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
using Nava.Entities.Media;
using Nava.Entities.MongoDb;
using Nava.Presentation.Models;
using Nava.WebFramework.Api;
using Artist = Nava.Entities.MongoDb.Artist;

namespace Nava.Presentation.Controllers.v2
{
    [ApiVersion("2")]
    public class FollowingsController : BaseController
    {
        private readonly IMongoRepository<User> _userRepository;
        private readonly IMongoRepository<Artist> _artistRepository;
        private readonly IMapper _mapper;

        public FollowingsController(IMongoRepository<User> userRepository, IMongoRepository<Artist> artistRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _artistRepository = artistRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Follow an artist with it's unique Id by an authorized user
        /// </summary>
        /// <param name="artistId">Artist's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(Follow) + "/{artistId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Follow(string artistId)
        {
            var username = User.Identity?.Name;
            var user = await _userRepository.FindOneAsync(a => a.UserName.Equals(username));

            if (user is null) throw new UnauthorizedAccessException();

            var artist = await _artistRepository.FindByIdAsync(artistId);

            if (artist is null) throw new NotFoundException();

            if (user.FollowingArtists.Exists(a => a.Equals(artist.Id)))
                throw new BadRequestException("شما این خواننده را از قبل دنبال کرده اید");

            artist.Followers.Add(user.Id);
            user.FollowingArtists.Add(artist.Id);

            await _artistRepository.ReplaceOneAsync(artist);
            await _userRepository.ReplaceOneAsync(user);

            return Ok();
        }

        /// <summary>
        /// UnFollow an artist with it's unique Id by an authorized user
        /// </summary>
        /// <param name="artistId">Artist's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(UnFollow) + "/{artistId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> UnFollow(string artistId)
        {
            var username = User.Identity?.Name;
            var user = await _userRepository.FindOneAsync(a => a.UserName.Equals(username));

            if (user is null) throw new UnauthorizedAccessException();

            var artist = await _artistRepository.FindByIdAsync(artistId);

            if (artist is null) throw new NotFoundException();

            if (!user.FollowingArtists.Exists(a => a.Equals(artist.Id)))
                throw new BadRequestException("شما این خواننده را از قبل دنبال نکرده اید");

            artist.Followers.Remove(user.Id);
            user.FollowingArtists.Remove(artist.Id);

            await _artistRepository.ReplaceOneAsync(artist);
            await _userRepository.ReplaceOneAsync(user);

            return Ok();
        }

        /// <summary>
        /// Get artists which the authorized user has followed them
        /// </summary>
        /// <param name="userId">User's unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetFollowings) + "/{userId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<MongoArtistResultDto>>> GetFollowings(string userId)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.FindOneAsync(a => a.UserName.Equals(authorizedUserName));

            if (authorizedUser is null) throw new UnauthorizedAccessException();

            if (authorizedUser.Id != new ObjectId(userId))
                if (!User.IsInRole(Role.Admin))
                    throw new UnauthorizedAccessException();

            var artists = new List<Artist>();

            foreach (var artistId in authorizedUser.FollowingArtists)
                artists.Add(await _artistRepository.FindByIdAsync(artistId.ToString()));

            var artistDtoList = new List<MongoArtistResultDto>(artists.Capacity);
            artistDtoList.AddRange(artists.Select(artist => MongoArtistResultDto.FromEntity(_mapper, artist)));

            return Ok(artistDtoList);
        }

        /// <summary>
        /// Get Followers of an artist by it's unique Id
        /// </summary>
        /// <param name="artistId">Artist's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetFollowers) + "/{artistId}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<MongoUserResultDto>>> GetFollowers(string artistId)
        {
            var artist = await _artistRepository.FindByIdAsync(artistId);

            if (artist is null)
                return NotFound();

            var users = new List<User>();

            foreach (var followerId in artist.Followers)
                users.Add(await _userRepository.FindByIdAsync(followerId.ToString()));

            var userDtoList = new List<MongoUserResultDto>(users.Capacity);
            userDtoList.AddRange(users.Select(user => MongoUserResultDto.FromEntity(_mapper, user)));

            return Ok(userDtoList);
        }
    }
}
