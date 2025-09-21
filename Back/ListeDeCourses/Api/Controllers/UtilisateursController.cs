using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ListeDeCourses.Api.Controllers;

[Authorize(Roles = "superuser")]
public class UtilisateursController
  : BaseController<UtilisateurReadDto, UtilisateurCreateDto, UtilisateurUpdateDto>
{
    public UtilisateursController(UtilisateurService service) : base(service) { }

    protected override string GetId(UtilisateurReadDto dto) => dto.Id;


    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UtilisateurCreateDto dto, CancellationToken ct)
    {
        var created = await Service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = GetId(created) }, created);
    }
}
