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

namespace Nava.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiResultFilter]
    public class LikesController : ControllerBase
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

        [HttpGet(nameof(Like) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Like(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.Table
                .Include(a => a.LikedUsers)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (media is null)
                throw new NotFoundException("مدیا یافت نشد");

            var username = User.Identity?.Name;
            var likedUser = await _userRepository.Table
                .FirstOrDefaultAsync(a =>
                    a.UserName.Equals(username), cancellationToken);

            if (likedUser is null)
                throw new BadRequestException();

            var mediaLike = new LikedMedia
            {
                MediaId = media.Id,
                UserId = likedUser.Id
            };

            if (media.LikedUsers.Any(a => a.MediaId.Equals(media.Id) && a.UserId.Equals(likedUser.Id)))
                throw new BadRequestException("این مدیا قبلا لایک شده است");

            media.LikedUsers.Add(mediaLike);

            await _mediaRepository.UpdateAsync(media, cancellationToken);

            return Ok();
        }

        [HttpGet(nameof(Dislike) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Dislike(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.Table
                .Include(a => a.LikedUsers)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (media is null)
                throw new NotFoundException("مدیا یافت نشد");

            var username = User.Identity?.Name;
            var likedUser = await _userRepository.Table
                .FirstOrDefaultAsync(a =>
                    a.UserName.Equals(username), cancellationToken);

            if (likedUser is null)
                throw new BadRequestException();

            var likedMedia = media.LikedUsers.FirstOrDefault(a => a.MediaId.Equals(media.Id) && a.UserId.Equals(likedUser.Id));

            if (likedMedia is null)
                throw new BadHttpRequestException("این مدیا قبلا لایک نشده است");

            media.LikedUsers.Remove(likedMedia);

            await _mediaRepository.UpdateAsync(media, cancellationToken);

            return Ok();
        }

        [HttpGet(nameof(GetLikedMedias) + "/{id}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<MediaResultDto>>> GetLikedMedias(int id, CancellationToken cancellationToken)
        {
            var authorizedUserName = User.Identity?.Name;
            var authorizedUser = await _userRepository.GetByUsernameAsync(authorizedUserName, cancellationToken);

            if (authorizedUser is null) throw new UnauthorizedAccessException();

            if (authorizedUser.Id != id)
                if (!User.IsInRole(Role.Admin))
                    throw new UnauthorizedAccessException("Restrict access.");

            var user = await _userRepository.TableNoTracking.Include(a => a.LikedMedias)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (user is null)
                throw new NotFoundException();

            var likedMedias = await _likedMediaRepository.TableNoTracking.Include(a => a.Media)
                .ThenInclude(a => a.Album)
                .ThenInclude(a => a.Artists)
                .Where(a => a.UserId.Equals(user.Id)).ToListAsync(cancellationToken);

            var likedMediaList = likedMedias.Select(likedMedia => likedMedia.Media).ToList();

            var mediaDtoList = new List<MediaResultDto>(likedMediaList.Capacity);
            mediaDtoList.AddRange(likedMediaList.Select(media => MediaResultDto.FromEntity(_mapper, media)));

            return Ok(mediaDtoList);
        }

        [HttpGet(nameof(GetLikedUsers) + "/{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<UserResultDto>>> GetLikedUsers(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.TableNoTracking
                .Include(a => a.LikedUsers)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (media is null)
                throw new NotFoundException();

            var likedMedias = await _likedMediaRepository.TableNoTracking
                .Include(a => a.User)
                .Where(a => a.MediaId.Equals(media.Id))
                .ToListAsync(cancellationToken);

            var likedUsersList = likedMedias.Select(likedMedia => likedMedia.User).ToList();

            var userDtoList = new List<UserResultDto>(likedUsersList.Capacity);
            userDtoList.AddRange(likedUsersList.Select(user => UserResultDto.FromEntity(_mapper, user)));

            return Ok(userDtoList);
        }
    }
}
