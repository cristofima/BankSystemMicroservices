using AutoMapper;
using Security.Application.Dtos;
using Security.Domain.Entities;

namespace Security.Application.Mapping;

public class UserContactMappingProfile : Profile
{
    public UserContactMappingProfile()
    {
        CreateMap<ApplicationUser, UserContactDto>()
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.ClientId))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName ?? string.Empty))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName ?? string.Empty))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber ?? string.Empty))
            .ForMember(
                dest => dest.IsActive,
                opt =>
                    opt.MapFrom(src =>
                        !src.LockoutEnabled
                        || src.LockoutEnd == null
                        || src.LockoutEnd <= DateTimeOffset.UtcNow
                    )
            )
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
    }
}
