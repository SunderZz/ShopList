namespace ListeDeCourses.Api.Common
{
    public record PagedResult<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        long TotalCount
    )
    {
        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 1.");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be >= 1.");
            if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount), "TotalCount must be >= 0.");

            return new(items, page, pageSize, totalCount);
        }

        public static PagedResult<T> Empty(int page = 1, int pageSize = 10) =>
            Create(Array.Empty<T>(), page, pageSize, 0);
    }
}
