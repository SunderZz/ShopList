using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ListeDeCourses.Api.Services;

namespace ListeDeCourses.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController<TReadDto, TCreateDto, TUpdateDto> : ControllerBase
{
    protected readonly IBaseService<TReadDto, TCreateDto, TUpdateDto> Service;

    protected BaseController(IBaseService<TReadDto, TCreateDto, TUpdateDto> service) => Service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await Service.GetAllAsync(ct));

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var item = await Service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "superuser")]
    public async Task<IActionResult> Create([FromBody] TCreateDto dto, CancellationToken ct)
    {
        var created = await Service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = GetId(created) }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "superuser")]
    public async Task<IActionResult> Update(string id, [FromBody] TUpdateDto dto, CancellationToken ct)
    {
        var updated = await Service.UpdateAsync(id, dto, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "superuser")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await Service.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    protected abstract string GetId(TReadDto dto);
}
