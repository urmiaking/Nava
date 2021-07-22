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
    public class MediasController : BaseController
    {
        private readonly IMongoRepository<Album> _albumRepository;
        private readonly IMongoRepository<Media> _mediaRepository;
        private readonly IMongoRepository<User> _userRepository;
        private readonly IMapper _mapper;
        private readonly IFileRepository _fileRepository;
        private const string MediaArtworkPath = "wwwroot\\media_avatars";
        private const string MediaFilePath = "wwwroot\\media_files";

        public MediasController(IMongoRepository<Album> albumRepository, IMongoRepository<Media> mediaRepository, IMongoRepository<User> userRepository, IMapper mapper, IFileRepository fileRepository)
        {
            _albumRepository = albumRepository;
            _mediaRepository = mediaRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _fileRepository = fileRepository;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public ActionResult<List<MongoMediaResultDto>> Get()
        {
            var medias = _mediaRepository.AsQueryable().ToList();
            return _mapper.Map<List<Media>, List<MongoMediaResultDto>>(medias);
        }

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<MongoMediaResultDto>> Get(string id)
        {
            var media = await _mediaRepository.FindByIdAsync(id);

            if (media is null)
                return NotFound();

            return MongoMediaResultDto.FromEntity(_mapper, media);
        }

        [HttpPost]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<MongoMediaResultDto>> Create([FromForm] MongoMediaDto dto)
        {
            var media = dto.ToEntity(_mapper);
            media.Id = ObjectId.GenerateNewId(DateTime.Now);
            media.AlbumId = new ObjectId(dto.StringAlbumId);

            var relatedAlbum = await _albumRepository.FindByIdAsync(dto.StringAlbumId);
                
            if (relatedAlbum is null)
                return BadRequest("آلبوم مدیا یافت نشد");

            if (relatedAlbum.IsSingle && relatedAlbum.Medias.Any())
                return BadRequest("این آلبوم تک مدیا بوده و امکان اضافه کردن مدیای دیگر به آن وجود ندارد");

            if (relatedAlbum.IsComplete)
                return BadRequest("وضعیت آلبوم تکیمل شده می باشد و امکان اضافه کردن مدیا به آن وجود ندارد");

            relatedAlbum.Medias.Add(media.Id);

            if (dto.ArtworkFile != null)
            {
                media.ArtworkPath = dto.ArtworkFile != null
                    ? _fileRepository.SaveFileAsync(dto.ArtworkFile, MediaArtworkPath).GetAwaiter().GetResult().FileName
                    : null;
            }
            else
                media.ArtworkPath = null;

            media.FilePath = dto.MediaFile != null 
                ? _fileRepository.SaveFileAsync(dto.MediaFile, MediaFilePath).GetAwaiter().GetResult().FileName
                : null;

            await _mediaRepository.InsertOneAsync(media);
            await _albumRepository.ReplaceOneAsync(relatedAlbum);

            return Ok(MongoMediaResultDto.FromEntity(_mapper, media));
        }
        
        [HttpDelete("{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Delete(string id)
        {
            var media = await _mediaRepository.FindByIdAsync(id);

            if (media is null)
                return BadRequest("مدیا یافت نشد");

            var relatedAlbum = await _albumRepository.FindByIdAsync(media.AlbumId.ToString());

            if (relatedAlbum is null)
                return BadRequest("آلبوم مدیا یافت نشد");

            relatedAlbum.Medias.Remove(media.Id);
            await _albumRepository.ReplaceOneAsync(relatedAlbum);

            var likedUsers = _userRepository.FilterBy(a => a.LikedMedias.Contains(media.Id)).ToList();
            if (likedUsers.Any())
            {
                foreach (var user in likedUsers)
                {
                    user.LikedMedias.Remove(media.Id);
                    await _userRepository.ReplaceOneAsync(user);
                }
            }

            var visitedUsers = _userRepository.FilterBy(a => a.VisitedMedias.Contains(media.Id)).ToList();
            if (visitedUsers.Any())
            {
                foreach (var user in visitedUsers)
                {
                    user.VisitedMedias.Remove(media.Id);
                    await _userRepository.ReplaceOneAsync(user);
                }
            }

            await _mediaRepository.DeleteByIdAsync(media.Id.ToString());

            _fileRepository.DeleteFile(Path.Combine(MediaArtworkPath, media.ArtworkPath));
            _fileRepository.DeleteFile(Path.Combine(MediaFilePath, media.FilePath));

            return Ok();
        }
        
        [HttpPut("{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<MongoMediaResultDto>> Update(string id, [FromForm] MongoMediaUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var media = await _mediaRepository.FindByIdAsync(id);

            if (media is null)
                return BadRequest("مدیا یافت نشد");

            var newAlbum = await _albumRepository.FindByIdAsync(dto.AlbumId);
            if (newAlbum is null)
                return BadRequest("آلبوم جدید مدیا یافت نشد");

            var previousAlbum = await _albumRepository.FindByIdAsync(media.AlbumId.ToString());

            if (previousAlbum.Id != newAlbum.Id)
            {
                previousAlbum.Medias.Remove(media.Id);
                await _albumRepository.ReplaceOneAsync(previousAlbum);

                newAlbum.Medias.Add(media.Id);
                await _albumRepository.ReplaceOneAsync(newAlbum);

                media.AlbumId = newAlbum.Id;
            }

            media.Isrc = dto.Isrc;
            media.Lyric = dto.Lyric;
            media.ReleaseDate = dto.ReleaseDate;
            media.Title = dto.Title;
            media.Type = dto.Type;
            media.TrackNumber = dto.TrackNumber;

            if (dto.ArtworkFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(MediaArtworkPath, media.ArtworkPath ?? ""));
                media.ArtworkPath = dto.ArtworkFile != null
                    ? _fileRepository.SaveFileAsync(dto.ArtworkFile, MediaArtworkPath).GetAwaiter().GetResult().FileName
                    : null;
            }

            if (dto.MediaFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(MediaFilePath, media.FilePath ?? ""));
                media.FilePath = dto.MediaFile != null
                    ? _fileRepository.SaveFileAsync(dto.MediaFile, MediaFilePath).GetAwaiter().GetResult().FileName
                    : null;
            }

            await _mediaRepository.ReplaceOneAsync(media);

            return MongoMediaResultDto.FromEntity(_mapper, media);
        }
        
        /// <summary>
        /// Get the actual Media file by it's unique Id
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetMediaFile) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<FileContentResult> GetMediaFile(string id)
        {
            var media = await _mediaRepository.FindByIdAsync(id);
            if (media is null)
                throw new BadRequestException("مدیا پیدا نشد");

            var path = _fileRepository.GetFilePath(MediaFilePath, media.FilePath);
            var contentType = _fileRepository.GetFileContentType(media.FilePath);
            var fileFormat = _fileRepository.GetFileExtension(media.FilePath);

            return File(await System.IO.File.ReadAllBytesAsync(path),
                contentType, $"{media.Title}{fileFormat}", true);
        }

        /// <summary>
        /// Get the actual Artwork file of a media by it's unique Id
        /// </summary>
        /// <param name="id">Media's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetArtworkFile) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<FileContentResult> GetArtworkFile(string id)
        {
            var media = await _mediaRepository.FindByIdAsync(id);
            if (media is null)
                throw new BadRequestException("مدیا پیدا نشد");

            var path = _fileRepository.GetFilePath(MediaArtworkPath, media.ArtworkPath);
            var contentType = _fileRepository.GetFileContentType(media.ArtworkPath);
            var fileFormat = _fileRepository.GetFileExtension(media.ArtworkPath);

            return File(await System.IO.File.ReadAllBytesAsync(path),
                contentType, $"{media.Title}{fileFormat}", true);
        }
    }
}
