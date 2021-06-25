﻿using System.Threading.Tasks;
using Nava.Entities;
using Nava.Entities.User;

namespace Nava.Services.Services
{
    public interface IJwtService
    {
        public Task<string> GenerateAsync(User user);
    }
}