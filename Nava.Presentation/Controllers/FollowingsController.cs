﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nava.Common;
using Nava.Common.Exceptions;
using Nava.Data.Contracts;
using Nava.Entities.Media;
using Nava.Presentation.Models;
using Nava.WebFramework.Api;
using Nava.WebFramework.Filters;

namespace Nava.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiResultFilter]
    public class FollowingsController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Artist> _artistRepository;
        private readonly IRepository<Following> _followingRepository;
        private readonly IMapper _mapper;

        public FollowingsController(IUserRepository userRepository, IRepository<Artist> artistRepository, IRepository<Following> followingRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _artistRepository = artistRepository;
            _followingRepository = followingRepository;
            _mapper = mapper;
        }

        [HttpGet(nameof(Follow) + "/{artistId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Follow(int artistId, CancellationToken cancellationToken)
        {
            var username = User.Identity?.Name;
            var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);

            if (user is null)
                throw new UnauthorizedAccessException();

            var artist = await _artistRepository.Table
                .Include(a => a.Followers)
                .FirstOrDefaultAsync(a => 
                    a.Id.Equals(artistId), cancellationToken);

            if (artist is null)
                throw new NotFoundException();

            var follower = new Following
            {
                ArtistId = artistId,
                UserId = user.Id
            };

            var followerExists = _followingRepository.TableNoTracking
                .Any(a=> a.ArtistId.Equals(follower.ArtistId) && a.UserId.Equals(follower.UserId));

            if (followerExists)
                throw new BadRequestException("شما این خواننده را از قبل دنبال کرده اید");

            artist.Followers.Add(follower);

            await _artistRepository.UpdateAsync(artist, cancellationToken);
            return Ok();
        }

        [HttpGet(nameof(UnFollow) + "/{artistId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> UnFollow(int artistId, CancellationToken cancellationToken)
        {
            var username = User.Identity?.Name;
            var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);

            if (user is null)
                throw new UnauthorizedAccessException();

            var artist = await _artistRepository.Table
                .Include(a => a.Followers)
                .FirstOrDefaultAsync(a =>
                    a.Id.Equals(artistId), cancellationToken);

            if (artist is null)
                throw new NotFoundException();

            var follower = await _followingRepository.Table
                .FirstOrDefaultAsync(a => a.ArtistId.Equals(artistId) && a.UserId.Equals(user.Id), cancellationToken);

            if (follower == null)
                throw new BadRequestException("شما این خواننده را از قبل دنبال نکرده اید");

            artist.Followers.Remove(follower);

            await _artistRepository.UpdateAsync(artist, cancellationToken);
            return Ok();
        }

        [HttpGet(nameof(GetFollowings) + "/{userId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<ArtistResultDto>>> GetFollowings(int userId, CancellationToken cancellationToken)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.GetByUsernameAsync(authorizedUserName, cancellationToken);

            if (authorizedUser is null) throw new UnauthorizedAccessException();

            if (authorizedUser.Id != userId)
                if (!User.IsInRole(Role.Admin))
                    throw new UnauthorizedAccessException();

            var followings = await _followingRepository.TableNoTracking
                .Include(a => a.Artist)
                .Where(a => a.UserId.Equals(userId))
                .ToListAsync(cancellationToken);

            var artists = followings.Select(following => following.Artist).ToList();

            var artistDtoList = new List<ArtistResultDto>(artists.Capacity);
            artistDtoList.AddRange(artists.Select(artist => ArtistResultDto.FromEntity(_mapper, artist)));

            return Ok(artistDtoList);
        }

        [HttpGet(nameof(GetFollowers) + "/{artistId}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<UserResultDto>>> GetFollowers(int artistId, CancellationToken cancellationToken)
        {
            var followers = await _followingRepository.TableNoTracking
                .Include(a => a.User)
                .Where(a => a.ArtistId.Equals(artistId))
                .ToListAsync(cancellationToken);

            var users = followers.Select(follower => follower.User).ToList();

            var userDtoList = new List<UserResultDto>(users.Capacity);
            userDtoList.AddRange(users.Select(user => UserResultDto.FromEntity(_mapper, user)));

            return Ok(userDtoList);
        }
    }
}
