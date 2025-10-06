using System.Threading;
using System.Threading.Tasks;

namespace ListeDeCourses.Api.Services
{
    public interface IItemCheckService<TReadDto>
    {
        Task<TReadDto?> SetItemCheckedAsync(
            string listId,
            string ingredientId,
            bool isChecked,
            CancellationToken ct = default
        );
    }
}
