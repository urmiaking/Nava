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
using Nava.Entities.User;
using Nava.Presentation.Models;
using Nava.WebFramework.Api;
using Role = Nava.Common.Role;

namespace Nava.Presentation.Controllers.v1
{
    [ApiVersion("1")]
    public class MediasController : CrudController<MediaDto, MediaResultDto, MediaUpdateDto, Media, int>
    {
        private readonly IFileRepository _fileRepository;
        private readonly string _mediaArtworkPath;
        private readonly string _mediaFilePath;
        private readonly IRepository<Media> _mediaRepository;
        private readonly IRepository<Album> _albumRepository;

        public MediasController(IRepository<Media> repository, IMapper mapper, IFileRepository fileRepository, IRepository<Album> albumRepository, IRepository<Media> mediaRepository)
            : base(repository, mapper)
        {
            _fileRepository = fileRepository;
            _albumRepository = albumRepository;
            _mediaRepository = mediaRepository;
            _mediaArtworkPath = "wwwroot\\media_avatars";
            _mediaFilePath = "wwwroot\\media_files";
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        public override Task<ActionResult<List<MediaResultDto>>> Get(CancellationToken cancellationToken) => base.Get(cancellationToken);

        [Authorize(AuthenticationSchemes = "Bearer")]
        public override Task<ApiResult<MediaResultDto>> Get(int id, CancellationToken cancellationToken) => base.Get(id, cancellationToken);

        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public override async Task<ApiResult<MediaResultDto>> Create([FromForm] MediaDto dto, CancellationToken cancellationToken)
        {
            dto.Id = 0;

            var relatedAlbum = await _albumRepository.TableNoTracking
                .Include(a => a.Medias)
                .FirstOrDefaultAsync(a => 
                    a.Id.Equals(dto.AlbumId), cancellationToken);

            if (relatedAlbum is null)
                return BadRequest("آلبوم مدیا یافت نشد");

            if (relatedAlbum.IsSingle && relatedAlbum.Medias.Any())
                return BadRequest("این آلبوم تک مدیا بوده و امکان اضافه کردن مدیای دیگر به آن وجود ندارد");

            if (relatedAlbum.IsComplete)
                return BadRequest("وضعیت آلبوم تکیمل شده می باشد و امکان اضافه کردن مدیا به آن وجود ندارد");

            if (dto.ArtworkFile != null)
                dto.ArtworkPath = dto.ArtworkFile != null
                    ? _fileRepository.SaveFileAsync(dto.ArtworkFile, _mediaArtworkPath).GetAwaiter().GetResult().FileName
                    : null;
            else
                dto.ArtworkPath = null;

            dto.FilePath = dto.MediaFile != null
                ? _fileRepository.SaveFileAsync(dto.MediaFile, _mediaFilePath).GetAwaiter().GetResult().FileName
                : null;

            return await base.Create(dto, cancellationToken);
        }

        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public override async Task<ApiResult> Delete(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.Table
                .Include(a => a.VisitedUsers)
                .Include(a => a.LikedUsers)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (media is null)
                return BadRequest("مدیا یافت نشد");

            media.LikedUsers = null;
            media.VisitedUsers = null;

            await _mediaRepository.DeleteAsync(media, cancellationToken);

            _fileRepository.DeleteFile(Path.Combine(_mediaArtworkPath, media.ArtworkPath));
            _fileRepository.DeleteFile(Path.Combine(_mediaFilePath, media.FilePath));

            return Ok();
        }

        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public override async Task<ApiResult<MediaResultDto>> Update(int id, [FromForm] MediaUpdateDto dto, CancellationToken cancellationToken)
        {
            if (dto.Id != id)
                return BadRequest();

            var media = await _mediaRepository.GetByIdAsync(cancellationToken, id);

            if (media is null)
                return BadRequest("مدیا یافت نشد");

            var relatedAlbum = await _albumRepository.GetByIdAsync(cancellationToken, dto.AlbumId);
            if (relatedAlbum is null)
                return BadRequest("آلبوم مدیا یافت نشد");

            if (dto.ArtworkFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(_mediaArtworkPath, media.ArtworkPath ?? ""));

                dto.ArtworkPath = dto.ArtworkFile != null
                    ? _fileRepository.SaveFileAsync(dto.ArtworkFile, _mediaArtworkPath).GetAwaiter().GetResult().FileName
                    : null;
            }
            else
                dto.ArtworkPath = null;

            if (dto.MediaFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(_mediaFilePath, media.FilePath ?? ""));

                dto.FilePath = dto.MediaFile != null
                    ? _fileRepository.SaveFileAsync(dto.MediaFile, _mediaFilePath).GetAwaiter().GetResult().FileName
                    : null;
            }

            return await base.Update(id, dto, cancellationToken);
        }

        /// <summary>
        /// Get the actual Media file by it's unique Id
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetMediaFile) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<FileContentResult> GetMediaFile(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.GetByIdAsync(cancellationToken, id);
            if (media is null)
                throw new BadRequestException("مدیا پیدا نشد");

            var path = _fileRepository.GetFilePath(_mediaFilePath, media.FilePath);
            var contentType = _fileRepository.GetFileContentType(media.FilePath);
            var fileFormat = _fileRepository.GetFileExtension(media.FilePath);

            return File(await System.IO.File.ReadAllBytesAsync(path, cancellationToken),
                contentType, $"{media.Title}{fileFormat}", true);
        }

        /// <summary>
        /// Get the actual Artwork file of a media by it's unique Id
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetArtworkFile) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<FileContentResult> GetArtworkFile(int id, CancellationToken cancellationToken)
        {
            var media = await _mediaRepository.GetByIdAsync(cancellationToken, id);
            if (media is null)
                throw new BadRequestException("مدیا پیدا نشد");

            var path = _fileRepository.GetFilePath(_mediaArtworkPath, media.ArtworkPath);
            var contentType = _fileRepository.GetFileContentType(media.ArtworkPath);
            var fileFormat = _fileRepository.GetFileExtension(media.ArtworkPath);

            return File(await System.IO.File.ReadAllBytesAsync(path, cancellationToken),
                contentType, $"{media.Title}{fileFormat}", true);
        }
    }
}
