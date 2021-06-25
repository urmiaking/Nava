using AutoMapper;
using System.Collections.Generic;
using Nava.WebFramework.CustomMapping;

namespace Nava.WebFramework.CustomMapping
{
    public class CustomMappingProfile : Profile
    {
        public CustomMappingProfile(IEnumerable<IHaveCustomMapping> haveCustomMappings)
        {
            foreach (var item in haveCustomMappings)
                item.CreateMappings(this);
        }
    }
}
