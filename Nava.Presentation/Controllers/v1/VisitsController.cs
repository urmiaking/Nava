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

namespace Nava.Presentation.Controllers.v1
{
    [ApiVersion("1")]
    public class VisitsController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Media> _mediaRepository;
        private readonly IRepository<VisitedMedia> _visitedMediaRepository;

        public VisitsController(IMapper mapper, IUserRepository userRepository, IRepository<Media> mediaRepository, IRepository<VisitedMedia> visitedMediaRepository)
        {
            _mapper = mapper;
            _userRepository = userRepository;
            _mediaRepository = mediaRepository;
            _visitedMediaRepository = visitedMediaRepository;
        }

        [HttpGet(nameof(Visit) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Visit(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.Table
                .Include(a => a.VisitedUsers)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (media is null)
                throw new NotFoundException("مدیا یافت نشد");

            var username = User.Identity?.Name;
            var visitedUser = await _userRepository.Table
                .FirstOrDefaultAsync(a =>
                    a.UserName.Equals(username), cancellationToken);

            if (visitedUser is null)
                throw new BadRequestException();

            var visitedMedia = new VisitedMedia
            {
                MediaId = media.Id,
                UserId = visitedUser.Id
            };
            var mediaVisitedBefore =
                media.VisitedUsers.FirstOrDefault(a => a.MediaId.Equals(media.Id) && a.UserId.Equals(visitedUser.Id));
            if (mediaVisitedBefore != null)
            {
                visitedMedia.TimeStamp = DateTime.Now;
                media.VisitedUsers.Remove(mediaVisitedBefore);
                media.VisitedUsers.Add(visitedMedia);
                await _mediaRepository.UpdateAsync(media, cancellationToken);

                return Ok();
            }

            media.VisitedUsers.Add(visitedMedia);

            await _mediaRepository.UpdateAsync(media, cancellationToken);

            return Ok();
        }

        [HttpGet(nameof(GetVisitedMedias) + "/{id}")]
        [Authorize(Roles = Role.User + "," + Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<MediaResultDto>>> GetVisitedMedias(int id, CancellationToken cancellationToken)
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

            var visitedMedias = await _visitedMediaRepository.TableNoTracking
                .Include(a => a.Media)
                .ThenInclude(a => a.Album)
                .ThenInclude(a => a.Artists)
                .Where(a => a.UserId.Equals(user.Id)).ToListAsync(cancellationToken);

            var visitedMediaList = visitedMedias.Select(visitedMedia => visitedMedia.Media).ToList();

            var mediaDtoList = new List<MediaResultDto>(visitedMediaList.Capacity);
            mediaDtoList.AddRange(visitedMediaList.Select(media => MediaResultDto.FromEntity(_mapper, media)));

            return Ok(mediaDtoList);
        }

        [HttpGet(nameof(GetVisitedUsers) + "/{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<List<UserResultDto>>> GetVisitedUsers(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.TableNoTracking
                .Include(a => a.LikedUsers)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (media is null)
                throw new NotFoundException();

            var visitedMedias = await _visitedMediaRepository.TableNoTracking
                .Include(a => a.User)
                .Where(a => a.MediaId.Equals(media.Id))
                .ToListAsync(cancellationToken);

            var visitedUsersList = visitedMedias.Select(visitedMedia => visitedMedia.User).ToList();

            var userDtoList = new List<UserResultDto>(visitedUsersList.Capacity);
            userDtoList.AddRange(visitedUsersList.Select(user => UserResultDto.FromEntity(_mapper, user)));

            return Ok(userDtoList);
        }
    }
}