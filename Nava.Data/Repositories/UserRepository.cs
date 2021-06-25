using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nava.Common;
using Nava.Common.Exceptions;
using Nava.Common.Utilities;
using Nava.Data.Contracts;
using Nava.Entities.User;

namespace Nava.Data.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository, IScopedDependency
    {
        public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public Task<User> GetByUserPassAsync(string username, string password, CancellationToken cancellationToken)
        {
            var passwordHash = SecurityHelper.GetSha256Hash(password);
            return Table.Where(p => p.UserName == username && p.PasswordHash == passwordHash).SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<User> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
            await TableNoTracking.FirstOrDefaultAsync(u => u.UserName.Equals(username), cancellationToken);

        public async Task CreateAsync(User user, string password, CancellationToken cancellationToken)
        {
            var userNameExists = await TableNoTracking
                .AnyAsync(u =>
                    u.UserName.Equals(user.UserName), cancellationToken);

            if (userNameExists)
                throw new BadRequestException("نام کاربری تکراری است");

            var passwordHash = SecurityHelper.GetSha256Hash(password);
            user.PasswordHash = passwordHash;
            await base.AddAsync(user, cancellationToken);
        }
    }
}
