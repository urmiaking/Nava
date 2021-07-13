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

namespace Nava.Presentation.Controllers.v1
{
    [ApiVersion("1")]
    public class AlbumsController : CrudController<AlbumDto, AlbumResultDto, AlbumUpdateDto, Album, int>
    {
        private readonly IFileRepository _fileRepository;
        private readonly string _albumsArtworkPath;
        private readonly IRepository<Album> _albumRepository;
        private readonly IRepository<Artist> _artistRepository;
        public AlbumsController(IRepository<Album> repository, IMapper mapper, IFileRepository fileRepository, IRepository<Artist> artistRepository)
            : base(repository, mapper)
        {
            _fileRepository = fileRepository;
            _artistRepository = artistRepository;
            _albumRepository = repository;
            _albumsArtworkPath = "wwwroot\\album_avatars";
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        public override Task<ApiResult<AlbumResultDto>> Get(int id, CancellationToken cancellationToken) => base.Get(id, cancellationToken);

        [Authorize(AuthenticationSchemes = "Bearer")]
        public override Task<ActionResult<List<AlbumResultDto>>> Get(CancellationToken cancellationToken) => base.Get(cancellationToken);

        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public override async Task<ApiResult<AlbumResultDto>> Create([FromForm] AlbumDto dto, CancellationToken cancellationToken)
        {
            dto.Id = 0;
            var artistIdsString = dto.ArtistIds.Split(',');
            var artistIds = artistIdsString.Select(int.Parse).ToList();

            var artistList = new List<Artist>();

            foreach (var artistId in artistIds)
            {
                var artist = await _artistRepository.GetByIdAsync(cancellationToken, artistId);
                if (artist != null)
                {
                    artistList.Add(artist);
                }
                else
                    return BadRequest("آیدی هنرمند اشتباه می باشد");
            }

            if (!artistList.Any())
            {
                return BadRequest("هنرمندی با آیدی داده شده پیدا نشد");
            }

            var album = dto.ToEntity(Mapper);

            album.Artists = artistList;

            var artworkSaveResult = await _fileRepository.SaveFileAsync(dto.ImageFile, _albumsArtworkPath);
            album.ArtworkPath = artworkSaveResult.FileCreationStatus switch
            {
                FileCreationStatus.Success => artworkSaveResult.FileName,
                _ => null
            };

            await Repository.AddAsync(album, cancellationToken);

            var resultDto = await _albumRepository.TableNoTracking.ProjectTo<AlbumResultDto>(Mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(p => p.Id.Equals(album.Id), cancellationToken);

            return resultDto;
        }

        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public override async Task<ApiResult> Delete(int id, CancellationToken cancellationToken)
        {
            var album = await _albumRepository.Table
                .Include(a=>a.Medias)
                .Include(a => a.Artists)
                .FirstOrDefaultAsync(a =>
                a.Id.Equals(id), cancellationToken);

            if (album is null)
                throw new NotFoundException();

            if (album.Medias.Any())
                return BadRequest("خطا در حذف! آلبوم دارای مدیا می باشد.");

            album.Artists = null;

            _fileRepository.DeleteFile(Path.Combine(_albumsArtworkPath, album.ArtworkPath));
            await _albumRepository.DeleteAsync(album, cancellationToken);

            return Ok();
        }

        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public override async Task<ApiResult<AlbumResultDto>> Update(int id, [FromForm] AlbumUpdateDto dto, CancellationToken cancellationToken)
        {
            dto.Id = id;

            var album = await _albumRepository.Table
                .Include(a => a.Artists)
                .FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

            if (album is null)
                return NotFound();

            var artistIdsString = dto.ArtistIds.Split(',');
            var artistIds = artistIdsString.Select(int.Parse).ToList();

            var artistList = new List<Artist>();

            foreach (var artistId in artistIds)
            {
                var artist = await _artistRepository.GetByIdAsync(cancellationToken, artistId);
                if (artist != null)
                {
                    artistList.Add(artist);
                }
            }

            if (!artistList.Any())
                return BadRequest("هنرمندی با آیدی داده شده پیدا نشد");

            album.Artists = artistList;

            if (dto.ImageFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(_albumsArtworkPath, album.ArtworkPath ?? ""));
                var avatarSaveResult = await _fileRepository.SaveFileAsync(dto.ImageFile, _albumsArtworkPath);

                dto.ArtworkPath = avatarSaveResult.FileCreationStatus switch
                {
                    FileCreationStatus.Success => avatarSaveResult.FileName,
                    FileCreationStatus.Failed => throw new BadRequestException("درج تصویر جدید با مشکل مواجه شد"),
                    _ => null
                };
            }
            else
                dto.ArtworkPath = null;
            
            album = dto.ToEntity(Mapper, album);

            await _albumRepository.UpdateAsync(album, cancellationToken);

            var resultDto = await Repository.TableNoTracking.ProjectTo<AlbumResultDto>(Mapper.ConfigurationProvider)
                .SingleOrDefaultAsync(p => p.Id.Equals(album.Id), cancellationToken);

            return resultDto;
        }

        /// <summary>
        /// Get the actual Artwork File of an album by it's unique Id
        /// </summary>
        /// <param name="id">Album's Unique Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetArtworkFile) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<FileContentResult> GetArtworkFile(int id, CancellationToken cancellationToken)
        {
            var album = await _albumRepository.GetByIdAsync(cancellationToken, id);
            if (album is null)
                throw new BadRequestException("آلبوم پیدا نشد");

            var path = _fileRepository.GetFilePath(_albumsArtworkPath, album.ArtworkPath);
            var contentType = _fileRepository.GetFileContentType(album.ArtworkPath);
            var fileFormat = _fileRepository.GetFileExtension(album.ArtworkPath);

            return File(await System.IO.File.ReadAllBytesAsync(path, cancellationToken),
                contentType, $"{album.Title}{fileFormat}", true);
        }
    }
}
