using AutoMapper;
using BankSystem.Account.Application.DTOs;
using BankSystem.Shared.Domain.ValueObjects;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.Mappings;

/// <summary>
/// AutoMapper profile for Account-related mappings
/// </summary>
public class AccountMappingProfile : Profile
{
    public AccountMappingProfile()
    {
        // Account mappings
        CreateMap<AccountEntity, AccountDto>()
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Balance.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Balance.Currency.Code))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => src.Type.ToString()));

        // Money value object mapping
        CreateMap<Money, decimal>()
            .ConvertUsing(src => src.Amount);

        // Currency mapping
        CreateMap<Currency, string>()
            .ConvertUsing(src => src.Code);
    }
}