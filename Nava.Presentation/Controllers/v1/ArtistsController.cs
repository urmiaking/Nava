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
    public class ArtistsController : CrudController<ArtistDto, ArtistResultDto, ArtistUpdateDto, Artist, int>
    {
        private readonly IFileRepository _fileRepository;
        private readonly string _artistsAvatarPath;
        private readonly IRepository<Artist> _artistRepository;
        public ArtistsController(IRepository<Artist> repository, IMapper mapper, IFileRepository fileRepository, IRepository<Artist> artistRepository)
            : base(repository, mapper)
        {
            _fileRepository = fileRepository;
            _artistRepository = artistRepository;
            _artistsAvatarPath = "wwwroot\\artists_avatars";
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        public override Task<ActionResult<List<ArtistResultDto>>> Get(CancellationToken cancellationToken) => base.Get(cancellationToken);

        [Authorize(AuthenticationSchemes = "Bearer")]
        public override Task<ApiResult<ArtistResultDto>> Get(int id, CancellationToken cancellationToken) => base.Get(id, cancellationToken);
        
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public override async Task<ApiResult<ArtistResultDto>> Create([FromForm] ArtistDto dto, CancellationToken cancellationToken)
        {
            dto.Id = 0;
            var avatarSaveResult = await _fileRepository.SaveFileAsync(dto.ImageFile, _artistsAvatarPath);
            dto.AvatarPath = avatarSaveResult.FileCreationStatus switch
            {
                FileCreationStatus.Success => avatarSaveResult.FileName,
                FileCreationStatus.Failed => throw new BadRequestException("خطا در ثبت نام"),
                _ => null
            };
            return await base.Create(dto, cancellationToken);
        }

        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public override async Task<ApiResult<ArtistResultDto>> Update(int id, [FromForm] ArtistUpdateDto dto, CancellationToken cancellationToken)
        {
            dto.Id = id;

            var artist = await _artistRepository.TableNoTracking.FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (artist is null)
                return NotFound();

            if (dto.ImageFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(_artistsAvatarPath, artist.AvatarPath ?? ""));
                var avatarSaveResult = await _fileRepository.SaveFileAsync(dto.ImageFile, _artistsAvatarPath);

                dto.AvatarPath = avatarSaveResult.FileCreationStatus switch
                {
                    FileCreationStatus.Success => avatarSaveResult.FileName,
                    FileCreationStatus.Failed => throw new BadRequestException("درج تصویر جدید با مشکل مواجه شد"),
                    _ => null
                };
            }
            else
                dto.AvatarPath = null;
            
            return await base.Update(id, dto, cancellationToken);
        }

        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public override async Task<ApiResult> Delete(int id, CancellationToken cancellationToken)
        {
            var artist = await Repository.Table
                .Include(a => a.Albums)
                .Include(a => a.Followers)
                .FirstOrDefaultAsync(a =>
                a.Id.Equals(id), cancellationToken);

            if (artist is null)
                throw new NotFoundException();

            if (artist.Albums.Any())
                throw new BadRequestException("آلبوم های هنرمند خالی نمی باشد.");

            artist.Followers = null;
            await Repository.UpdateAsync(artist, cancellationToken);

            _fileRepository.DeleteFile(Path.Combine(_artistsAvatarPath, artist.AvatarPath));

            return await base.Delete(id, cancellationToken);
        }

        /// <summary>
        /// Get the actual Avatar of an artist by it's unique Id
        /// </summary>
        /// <param name="id">Artist's Unique Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetArtistAvatar) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<FileContentResult> GetArtistAvatar(int id, CancellationToken cancellationToken)
        {
            var artist = await _artistRepository.GetByIdAsync(cancellationToken, id);
            if (artist is null)
                throw new BadRequestException("خواننده پیدا نشد");

            var path = _fileRepository.GetFilePath(_artistsAvatarPath, artist.AvatarPath);
            var contentType = _fileRepository.GetFileContentType(artist.AvatarPath);
            var fileFormat = _fileRepository.GetFileExtension(artist.AvatarPath);

            return File(await System.IO.File.ReadAllBytesAsync(path, cancellationToken),
                contentType, $"{artist.ArtisticName}{fileFormat}", true);
        }
    }
}
