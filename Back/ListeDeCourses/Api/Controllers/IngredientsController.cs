using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Services;

namespace ListeDeCourses.Api.Controllers;

public class IngredientsController : BaseController<IngredientReadDto, IngredientCreateDto, IngredientUpdateDto>
{
    public IngredientsController(IngredientService service) : base(service) { }

    protected override string GetId(IngredientReadDto dto) => dto.Id;
}
