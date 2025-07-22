using AutoMapper;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Mappings;
using BankSystem.Shared.Domain.ValueObjects;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.UnitTests.Mappings;

public class AccountMappingProfileTests
{
    private readonly MapperConfiguration _configuration;
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
        Assert.True(true); // Explicit success assertion for test clarity
    }

    [Theory]
    [InlineData(typeof(AccountEntity), typeof(AccountDto))]
    [InlineData(typeof(Money), typeof(decimal))]
    [InlineData(typeof(Currency), typeof(string))]
    public void Map_SourceToDestination_ExistConfiguration(Type origin, Type destination)
    {
        var instance = origin.GetConstructor(Type.EmptyTypes) != null
            ? Activator.CreateInstance(origin)!
            : GetDefaultInstance(origin);

        var result = _mapper.Map(instance, origin, destination);
        Assert.NotNull(result);
    }

    private static object GetDefaultInstance(Type type)
    {
        if (type == typeof(Money))
            return Money.Zero(Currency.USD);
        if (type == typeof(Currency))
            return Currency.USD;
        if (type == typeof(AccountEntity))
            return Activator.CreateInstance(type, nonPublic: true)!;

        throw new NotImplementedException($"No default instance defined for {type}");
    }
}