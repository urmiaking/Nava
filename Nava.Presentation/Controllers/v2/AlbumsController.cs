using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace Nava.Presentation.Controllers.v2
{
    [ApiVersion("2")]
    public class AlbumsController : BaseController
    {
        private readonly IMongoRepository<Album> _albumRepository;
        private readonly IMongoRepository<Artist> _artistRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IMapper _mapper;
        private const string AlbumsArtworkPath = "wwwroot\\album_avatars";

        public AlbumsController(IMongoRepository<Album> albumRepository, IFileRepository fileRepository, IMapper mapper, IMongoRepository<Artist> artistRepository)
        {
            _albumRepository = albumRepository;
            _fileRepository = fileRepository;
            _mapper = mapper;
            _artistRepository = artistRepository;
        }

        
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<MongoAlbumResultDto>> Get(string id)
        {
            var album = await _albumRepository.FindByIdAsync(id);

            if (album is null)
                return NotFound();

            return MongoAlbumResultDto.FromEntity(_mapper, album);
        }
        
        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public ActionResult<List<MongoAlbumResultDto>> Get()
        {
            var albums = _albumRepository.AsQueryable().ToList();
            return _mapper.Map<List<Album>, List<MongoAlbumResultDto>>(albums);
        }
        
        [HttpPost]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<MongoAlbumResultDto>> Create([FromForm] MongoAlbumDto dto)
        {
            var album = dto.ToEntity(_mapper);

            album.Id = ObjectId.GenerateNewId(DateTime.Now);

            var artistIds = dto.ArtistIds.Split(',');

            foreach (var artistId in artistIds)
            {
                var artist = await _artistRepository.FindByIdAsync(artistId);
                if (artist != null)
                {
                    album.Artists.Add(artist.Id);
                    artist.Albums.Add(album.Id); // TWO-way referencing
                    await _artistRepository.ReplaceOneAsync(artist);
                }
            }

            if (!album.Artists.Any())
                return BadRequest("لطفا حداقل یک هنرمند به آلبوم اضافه کنید");

            album.ArtworkPath = dto.ImageFile != null
                ? _fileRepository.SaveFileAsync(dto.ImageFile, AlbumsArtworkPath).GetAwaiter().GetResult().FileName
                : null;

            await _albumRepository.InsertOneAsync(album);

            return Ok(MongoAlbumResultDto.FromEntity(_mapper, album));
        }
        
        [HttpDelete("{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Delete(string id)
        {
            var album = await _albumRepository.FindByIdAsync(id);

            if (album is null)
                return NotFound();

            if (album.Medias.Any())
                return BadRequest("خطا در حذف! آلبوم دارای مدیا می باشد.");

            foreach (var artistId in album.Artists)
            {
                var artist = await _artistRepository.FindByIdAsync(artistId.ToString());
                artist.Albums.Remove(album.Id);
                await _artistRepository.ReplaceOneAsync(artist);
            }

            _fileRepository.DeleteFile(Path.Combine(AlbumsArtworkPath, album.ArtworkPath));
            await _albumRepository.DeleteByIdAsync(id);

            return Ok();
        }
       
        [HttpPut("{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<MongoAlbumResultDto>> Update(string id, [FromForm] MongoAlbumUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var album = await _albumRepository.FindByIdAsync(id);

            if (album is null)
                return NotFound();

            album.IsSingle = dto.IsSingle;
            album.IsComplete = dto.IsComplete;
            album.Copyright = dto.Copyright;
            album.Genre = dto.Genre;
            album.ReleaseDate = dto.ReleaseDate;
            album.Title = dto.Title;

            var artistIds = dto.ArtistIds.Split(',');

            album.Artists.Clear();

            foreach (var artistId in artistIds)
            {
                var artist = await _artistRepository.FindByIdAsync(artistId);
                if (artist != null)
                {
                    album.Artists.Add(artist.Id);
                    if (!artist.Albums.Exists(a => a.Equals(album.Id)))
                    {
                        artist.Albums.Add(album.Id);
                        await _artistRepository.ReplaceOneAsync(artist);
                    }
                }
            }

            if (!album.Artists.Any())
                return BadRequest("لطفا حداقل یک هنرمند به آلبوم اضافه کنید");

            if (dto.ImageFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(AlbumsArtworkPath, album.ArtworkPath ?? ""));

                album.ArtworkPath = dto.ImageFile != null
                    ? _fileRepository.SaveFileAsync(dto.ImageFile, AlbumsArtworkPath).GetAwaiter().GetResult().FileName
                    : null;
            }

            await _albumRepository.ReplaceOneAsync(album);

            return MongoAlbumResultDto.FromEntity(_mapper, album);
        }
         
        /// <summary>
        /// Get the actual Artwork File of an album by it's unique Id
        /// </summary>
        /// <param name="id">Album's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetArtworkFile) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<FileContentResult> GetArtworkFile(string id)
        {
            var album = await _albumRepository.FindByIdAsync(id);
            if (album is null)
                throw new BadRequestException("آلبوم پیدا نشد");

            var path = _fileRepository.GetFilePath(AlbumsArtworkPath, album.ArtworkPath);
            var contentType = _fileRepository.GetFileContentType(album.ArtworkPath);
            var fileFormat = _fileRepository.GetFileExtension(album.ArtworkPath);

            return File(await System.IO.File.ReadAllBytesAsync(path),
                contentType, $"{album.Title}{fileFormat}", true);
        }
         
        
    }
}
