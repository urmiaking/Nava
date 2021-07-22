using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
    public class ArtistsController : BaseController
    {
        private readonly IMongoRepository<Artist> _artistRepository;
        private readonly IMongoRepository<User> _userRepository;

        private readonly IFileRepository _fileRepository;
        private readonly IMapper _mapper;
        private const string ArtistsAvatarPath = "wwwroot\\artists_avatars";

        public ArtistsController(IMongoRepository<Artist> artistRepository, IFileRepository fileRepository, IMapper mapper, IMongoRepository<User> userRepository)
        {
            _artistRepository = artistRepository;
            _fileRepository = fileRepository;
            _mapper = mapper;
            _userRepository = userRepository;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public ActionResult<List<MongoArtistResultDto>> Get()
        {
            var artists = _artistRepository.AsQueryable().ToList();
            return _mapper.Map<List<Artist>, List<MongoArtistResultDto>>(artists);
        }

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<MongoArtistResultDto>> Get(string id)
        {
            var artist = await _artistRepository.FindByIdAsync(id);
            if (artist is null)
                return NotFound();

            return Ok(MongoArtistResultDto.FromEntity(_mapper, artist));
        }
        
        [HttpPost]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<MongoArtistResultDto>> Create([FromForm] MongoArtistDto dto)
        {
            var artist = dto.ToEntity(_mapper);
            artist.Id = ObjectId.GenerateNewId(DateTime.Now);
            artist.AvatarPath = dto.ImageFile != null
                ? _fileRepository.SaveFileAsync(dto.ImageFile, ArtistsAvatarPath).GetAwaiter().GetResult()
                    .FileName
                : null;
            await _artistRepository.InsertOneAsync(artist);
            return Ok(MongoArtistResultDto.FromEntity(_mapper, artist));
        }
        
        [HttpPut("{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult<MongoArtistResultDto>> Update(string id, [FromForm] MongoArtistUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var artist = await _artistRepository.FindByIdAsync(id);

            if (artist is null)
                return NotFound();

            artist.ArtisticName = dto.ArtisticName;
            artist.Bio = dto.Bio;
            artist.BirthDate = dto.BirthDate;
            artist.FullName = dto.FullName;

            if (dto.ImageFile != null)
            {
                _fileRepository.DeleteFile(Path.Combine(ArtistsAvatarPath, artist.AvatarPath ?? ""));

                artist.AvatarPath = dto.ImageFile != null
                    ? _fileRepository.SaveFileAsync(dto.ImageFile, ArtistsAvatarPath).GetAwaiter().GetResult().FileName
                    : null;
            }

            await _artistRepository.ReplaceOneAsync(artist);
            return Ok(MongoArtistResultDto.FromEntity(_mapper, artist));
        }
        
        
        [HttpDelete("{id}")]
        [Authorize(Roles = Role.Admin, AuthenticationSchemes = "Bearer")]
        public async Task<ApiResult> Delete(string id)
        {
            var artist = await _artistRepository.FindByIdAsync(id);

            if (artist is null)
                return NotFound();

            if (artist.Albums.Any())
                return BadRequest("آلبوم های هنرمند خالی نمی باشد.");

            var followers = _userRepository.FilterBy(a => 
                a.FollowingArtists.Contains(artist.Id)).ToList();

            foreach (var follower in followers)
            {
                follower.FollowingArtists.Remove(artist.Id);
                await _userRepository.ReplaceOneAsync(follower);
            }

            _fileRepository.DeleteFile(Path.Combine(ArtistsAvatarPath, artist.AvatarPath));

            await _artistRepository.DeleteByIdAsync(id);
            return Ok();
        }
        
        /// <summary>
        /// Get the actual Avatar of an artist by it's unique Id
        /// </summary>
        /// <param name="id">Artist's Unique Id</param>
        /// <returns></returns>
        [HttpGet(nameof(GetArtistAvatar) + "/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<FileContentResult> GetArtistAvatar(string id)
        {
            var artist = await _artistRepository.FindByIdAsync(id);
            if (artist is null)
                throw new BadRequestException("خواننده پیدا نشد");

            var path = _fileRepository.GetFilePath(ArtistsAvatarPath, artist.AvatarPath);
            var contentType = _fileRepository.GetFileContentType(artist.AvatarPath);
            var fileFormat = _fileRepository.GetFileExtension(artist.AvatarPath);

            return File(await System.IO.File.ReadAllBytesAsync(path),
                contentType, $"{artist.ArtisticName}{fileFormat}", true);
        }
    }
}
