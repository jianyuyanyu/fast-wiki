using FastWiki.Service.Application.Users.Queries;
using FastWiki.Service.Contracts.Users.Dto;
using FastWiki.Service.Domain.Users.Repositories;

namespace FastWiki.Service.Application.Users;

public sealed class UserQueryHandler(IUserRepository userRepository, IMapper mapper)
{
    [EventHandler]
    public async Task UserInfoAsync(UserInfoQuery query)
    {
        var dto = await userRepository.FindAsync(x => x.Account == query.Account);

        if (dto == null)
        {
            throw new UserFriendlyException("账号不存在");
        }

        if (!dto.CheckCipher(query.Pass))
        {
            throw new UserFriendlyException("密码错误");
        }

        query.Result = mapper.Map<UserDto>(dto);
    }

    [EventHandler]
    public async Task UserListAsync(UserListQuery query)
    {
        var list = await userRepository.GetPaginatedListAsync(x => x.Account.Contains(query.Keyword), query.Page,
            query.PageSize);
        
        var total = await userRepository.GetCountAsync(x => x.Account.Contains(query.Keyword));
        
        query.Result = new PaginatedListBase<UserDto>
        {
            Total = total,
            Result = mapper.Map<List<UserDto>>(list)
        };
    }
}