using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nava.Entities.User;

namespace Nava.Data.Contracts
{
    public interface IUserRepository : IRepository<User>
    {
        public Task<User> GetByUserPassAsync(string username, string password, CancellationToken cancellationToken);

        public Task<User> GetByUsernameAsync(string username, CancellationToken cancellationToken);

        public Task CreateAsync(User user, string password, CancellationToken cancellationToken);
    }
}
