namespace ListeDeCourses.Api.Services
{
    public interface IBaseService<TReadDto, TCreateDto, TUpdateDto>
    {
        Task<IEnumerable<TReadDto>> GetAllAsync(CancellationToken ct = default);
        Task<TReadDto?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<TReadDto> CreateAsync(TCreateDto dto, CancellationToken ct = default);
        Task<TReadDto?> UpdateAsync(string id, TUpdateDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    }
}
