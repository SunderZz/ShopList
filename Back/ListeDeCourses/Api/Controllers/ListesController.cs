using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Services;

namespace ListeDeCourses.Api.Controllers;

public class ListesController : BaseController<ListeReadDto, ListeCreateDto, ListeUpdateDto>
{
    public ListesController(ListeService service) : base(service) { }

    protected override string GetId(ListeReadDto dto) => dto.Id;
}
