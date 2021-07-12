using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Nava.Data.Contracts;
using Nava.Entities.Media;
using Nava.Entities.User;
using Nava.Presentation.Models;
using Nava.Services.Services;
using Nava.WebFramework.Api;

namespace Nava.Presentation.Controllers.v2
{
    [ApiVersion("2")]
    public class UserController : BaseController
    {
        private readonly IMongoRepository<Entities.MongoDb.User> _mongoRepository;
        public UserController(IMongoRepository<Entities.MongoDb.User> mongoRepository)
        {
            _mongoRepository = mongoRepository;
        }

        /*[HttpGet(nameof(DeactivateUserAccount))]
        public Task<ApiResult> DeactivateUserAccount(int userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        [HttpPost(nameof(Token))]
        public Task<ActionResult> Token(TokenRequest tokenRequest, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public Task<ActionResult<List<UserResultDto>>> Get(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{id}")]
        public  Task<ActionResult<User>> Get(int id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }*/

        [HttpPost]
        [AllowAnonymous]
        public Task<ApiResult<UserResultDto>> Create(Entities.MongoDb.User user, CancellationToken cancellationToken)
        {
            _mongoRepository.InsertOne(user);
            throw new NotImplementedException();
        }

        /*[HttpPut]
        public Task<ApiResult<UserResultDto>> Update(int id, UserUpdateDto dto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        [HttpDelete]
        public Task<ApiResult> Delete(int id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }*/
    }
}
