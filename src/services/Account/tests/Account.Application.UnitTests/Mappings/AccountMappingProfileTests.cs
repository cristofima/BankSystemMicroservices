using AutoMapper;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Mappings;
using BankSystem.Shared.Domain.ValueObjects;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.UnitTests.Mappings;

public class AccountMappingProfileTests
{
    private readonly IConfigurationProvider _configuration;
    private readonly IMapper _mapper;

    public AccountMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AccountMappingProfile>();
        });

        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void ShouldBeValidConfiguration()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Theory]
    [InlineData(typeof(AccountEntity), typeof(AccountDto))]
    [InlineData(typeof(Money), typeof(decimal))]
    [InlineData(typeof(Currency), typeof(string))]
    public void Map_SourceToDestination_ExistConfiguration(Type origin, Type destination)
    {
        var instance = Activator.CreateInstance(origin, nonPublic: true);
        _mapper.Map(instance, origin, destination);
    }
}