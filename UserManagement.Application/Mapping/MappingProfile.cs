﻿using AutoMapper;
using UserManagement.Application.DTOs;
using UserManagement.Core.Entities;

namespace UserManagement.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<CreateUserDto, User>();
            CreateMap<UpdateUserDto, User>();
        }
    }
}



