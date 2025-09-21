using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace ListeDeCourses.Api.Controllers;

public class UtilisateursController : BaseController<UtilisateurReadDto, UtilisateurCreateDto, UtilisateurUpdateDto>
{
    public UtilisateursController(UtilisateurService service) : base(service) { }

    protected override string GetId(UtilisateurReadDto dto) => dto.Id;
}
