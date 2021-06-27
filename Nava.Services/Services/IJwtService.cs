using System.Threading.Tasks;
using Nava.Entities;
using Nava.Entities.User;

namespace Nava.Services.Services
{
    public interface IJwtService
    {
        public Task<AccessToken> GenerateAsync(User user);
    }
}