using Microsoft.AspNetCore.Http;
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

namespace Nava.Presentation.Controllers.v1
{
    [ApiVersion("1")]
    public class LikesController : BaseController
    {
        private readonly IRepository<LikedMedia> _likedMediaRepository;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Media> _mediaRepository;

        public LikesController(IRepository<LikedMedia> likedMediaRepository, IMapper mapper, IUserRepository userRepository, IRepository<Media> mediaRepository)
        {
            _likedMediaRepository = likedMediaRepository;
            _mapper = mapper;
            _userRepository = userRepository;
            _mediaRepository = mediaRepository;
        }

        /// <summary>
        /// Like a media with it's unique Id by an authorized user
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(Like) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Like(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.Table
                .Include(a => a.LikedUsers)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (media is null)
                return NotFound();

            var username = User.Identity?.Name;
            var likedUser = await _userRepository.Table
                .FirstOrDefaultAsync(a =>
                    a.UserName.Equals(username), cancellationToken);

            if (likedUser is null)
                return BadRequest();

            var mediaLike = new LikedMedia
            {
                MediaId = media.Id,
                UserId = likedUser.Id
            };

            if (media.LikedUsers.Any(a => a.MediaId.Equals(media.Id) && a.UserId.Equals(likedUser.Id)))
                return BadRequest("این مدیا قبلا لایک شده است");

            media.LikedUsers.Add(mediaLike);

            await _mediaRepository.UpdateAsync(media, cancellationToken);

            return Ok();
        }

        /// <summary>
        /// Dislike a media with it's unique Id by an authorized user
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(Dislike) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Dislike(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.Table
                .Include(a => a.LikedUsers)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (media is null)
                return NotFound();

            var username = User.Identity?.Name;
            var likedUser = await _userRepository.Table
                .FirstOrDefaultAsync(a =>
                    a.UserName.Equals(username), cancellationToken);

            if (likedUser is null)
                return BadRequest();

            var likedMedia = media.LikedUsers.FirstOrDefault(a => a.MediaId.Equals(media.Id) && a.UserId.Equals(likedUser.Id));

            if (likedMedia is null)
                return BadRequest("این مدیا قبلا لایک نشده است");

            media.LikedUsers.Remove(likedMedia);

            await _mediaRepository.UpdateAsync(media, cancellationToken);

            return Ok();
        }

        /// <summary>
        /// Get medias which has been liked by a user with it's unique Id
        /// </summary>
        /// <param name="id">User's Unique Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetLikedMedias) + "/{id}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<MediaResultDto>>> GetLikedMedias(int id, CancellationToken cancellationToken)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.GetByUsernameAsync(authorizedUserName, cancellationToken);

            if (authorizedUser is null) return Unauthorized();

            if (authorizedUser.Id != id)
                if (!User.IsInRole(Role.Admin))
                    return Forbid();

            var user = await _userRepository.TableNoTracking.Include(a => a.LikedMedias)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (user is null)
                return NotFound();

            var likedMedias = await _likedMediaRepository.TableNoTracking.Include(a => a.Media)
                .ThenInclude(a => a.Album)
                .ThenInclude(a => a.Artists)
                .Where(a => a.UserId.Equals(user.Id))
                .OrderByDescending(a => a.TimeStamp)
                .ToListAsync(cancellationToken);

            var likedMediaList = likedMedias.Select(likedMedia => likedMedia.Media).ToList();

            var mediaDtoList = new List<MediaResultDto>(likedMediaList.Capacity);
            mediaDtoList.AddRange(likedMediaList.Select(media => MediaResultDto.FromEntity(_mapper, media)));

            return Ok(mediaDtoList);
        }

        /// <summary>
        /// Get Users which had liked a media with it's unique Id
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetLikedUsers) + "/{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<UserResultDto>>> GetLikedUsers(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.TableNoTracking
                .Include(a => a.LikedUsers)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (media is null)
                return NotFound();

            var likedMedias = await _likedMediaRepository.TableNoTracking
                .Include(a => a.User)
                .Where(a => a.MediaId.Equals(media.Id))
                .OrderByDescending(a => a.TimeStamp)
                .ToListAsync(cancellationToken);

            var likedUsersList = likedMedias.Select(likedMedia => likedMedia.User).ToList();

            var userDtoList = new List<UserResultDto>(likedUsersList.Capacity);
            userDtoList.AddRange(likedUsersList.Select(user => UserResultDto.FromEntity(_mapper, user)));

            return Ok(userDtoList);
        }
    }
}
