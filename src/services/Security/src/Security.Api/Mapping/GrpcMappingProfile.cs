using AutoMapper;
using BankSystem.Security.Api.Protos;
using Security.Application.Dtos;

namespace Security.Api.Mapping;

/// <summary>
/// AutoMapper profile for mapping between application DTOs and gRPC proto messages
/// Located in API layer as this is where gRPC concerns are handled
/// Follows Clean Architecture by keeping gRPC-specific mappings at the API boundary
/// </summary>
public class GrpcMappingProfile : Profile
{
    public GrpcMappingProfile()
    {
        CreateMap<UserContactDto, UserContactInfo>()
            .ForMember(
                dest => dest.CustomerId,
                opt => opt.MapFrom(src => src.CustomerId.ToString())
            )
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(
                dest => dest.PhoneNumber,
                opt => opt.MapFrom(src => src.PhoneNumber ?? string.Empty)
            )
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
    }
}
