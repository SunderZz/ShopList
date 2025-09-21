using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Services;

namespace ListeDeCourses.Api.Controllers;

public class PlatsController : BaseController<PlatReadDto, PlatCreateDto, PlatUpdateDto>
{
    public PlatsController(PlatService service) : base(service) { }

    protected override string GetId(PlatReadDto dto) => dto.Id;
}
