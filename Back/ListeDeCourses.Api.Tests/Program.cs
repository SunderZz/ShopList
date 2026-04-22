using System.Net;
using System.Security.Claims;
using ListeDeCourses.Api.Common;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Repositories;
using ListeDeCourses.Api.Services;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Tests;

public static class Program
{
    public static async Task<int> Main()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            ("anonymous user cannot list shopping lists", AnonymousUserCannotListShoppingLists),
            ("standard user gets only owned shopping lists", StandardUserGetsOnlyOwnedShoppingLists),
            ("admin user gets every shopping list", AdminUserGetsEveryShoppingList),
            ("standard user cannot read a foreign shopping list by id", StandardUserCannotReadForeignShoppingListById),
            ("standard user cannot mutate a foreign shopping list", StandardUserCannotMutateForeignShoppingList),
            ("standard user can check an item on an owned shopping list", StandardUserCanCheckOwnedShoppingListItem),
            ("created shopping list is attached to the current user", CreatedShoppingListIsAttachedToCurrentUser),
        };

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {test.Name}");
                Console.Error.WriteLine(ex);
            }
        }

        return failures == 0 ? 0 : 1;
    }

    private static async Task AnonymousUserCannotListShoppingLists()
    {
        var service = CreateService(new[] { List("list-a", ownerId: "user-a") }, principal: null);

        await Assert.ThrowsDomainAsync(
            async () => await service.GetAllAsync(),
            HttpStatusCode.Unauthorized);
    }

    private static async Task StandardUserGetsOnlyOwnedShoppingLists()
    {
        var service = CreateService(new[]
        {
            List("owned", ownerId: "user-a"),
            List("legacy-owned", legacyUserId: "user-a"),
            List("foreign", ownerId: "user-b"),
        }, User("user-a"));

        var result = (await service.GetAllAsync()).OrderBy(x => x.Id, StringComparer.Ordinal).ToList();

        Assert.Equal(2, result.Count);
        Assert.SequenceEqual(new[] { "legacy-owned", "owned" }, result.Select(x => x.Id));
        Assert.True(result.All(x => x.OwnerId == "user-a"), "Every visible list should belong to the current user.");
    }

    private static async Task AdminUserGetsEveryShoppingList()
    {
        var service = CreateService(new[]
        {
            List("user-a-list", ownerId: "user-a"),
            List("user-b-list", ownerId: "user-b"),
            List("legacy-list", legacyUserId: "legacy-user"),
        }, Admin("admin"));

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(3, result.Count);
    }

    private static async Task StandardUserCannotReadForeignShoppingListById()
    {
        var service = CreateService(new[]
        {
            List("foreign", ownerId: "user-b"),
        }, User("user-a"));

        await Assert.ThrowsDomainAsync(
            async () => await service.GetByIdAsync("foreign"),
            HttpStatusCode.Forbidden);
    }

    private static async Task StandardUserCannotMutateForeignShoppingList()
    {
        var service = CreateService(new[]
        {
            List("foreign", ownerId: "user-b", items: new[] { Item("ingredient-a") }),
        }, User("user-a"));

        await Assert.ThrowsDomainAsync(
            async () => await service.UpdateAsync("foreign", new ListeUpdateDto { Name = "blocked" }),
            HttpStatusCode.Forbidden);

        await Assert.ThrowsDomainAsync(
            async () => await service.DeleteAsync("foreign"),
            HttpStatusCode.Forbidden);

        await Assert.ThrowsDomainAsync(
            async () => await service.SetItemCheckedAsync("foreign", "ingredient-a", true),
            HttpStatusCode.Forbidden);
    }

    private static async Task StandardUserCanCheckOwnedShoppingListItem()
    {
        var lists = new InMemoryListeRepository(new[]
        {
            List("owned", ownerId: "user-a", items: new[] { Item("ingredient-a", isChecked: false) }),
        });
        var service = CreateService(lists, User("user-a"));

        var result = await service.SetItemCheckedAsync("owned", "ingredient-a", true);

        Assert.NotNull(result, "Expected the updated shopping list.");
        Assert.True(result!.Items.Single().Checked == true, "Returned item should be checked.");
        Assert.True(lists.Stored("owned")!.Items.Single().Checked, "Stored item should be checked.");
    }

    private static async Task CreatedShoppingListIsAttachedToCurrentUser()
    {
        var lists = new InMemoryListeRepository(Array.Empty<Liste>());
        var service = CreateService(lists, User("user-a"));

        var result = await service.CreateAsync(new ListeCreateDto
        {
            Name = "created",
            Date = new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc),
            Items = new List<ListeItemDto>(),
            DishIds = new List<string>(),
        });

        var stored = lists.Stored(result.Id);
        Assert.NotNull(stored, "Expected the created list to be stored.");
        Assert.Equal("user-a", result.OwnerId);
        Assert.Equal("user-a", stored!.OwnerId);
        Assert.Equal(null, stored.LegacyUserId);
    }

    private static ListeService CreateService(IEnumerable<Liste> seed, ClaimsPrincipal? principal) =>
        CreateService(new InMemoryListeRepository(seed), principal);

    private static ListeService CreateService(InMemoryListeRepository lists, ClaimsPrincipal? principal)
    {
        var context = new DefaultHttpContext();
        if (principal is not null) context.User = principal;

        return new ListeService(
            lists,
            new InMemoryPlatRepository(Array.Empty<Plat>()),
            new InMemoryIngredientRepository(Array.Empty<Ingredient>()),
            new HttpContextAccessor { HttpContext = context });
    }

    private static ClaimsPrincipal User(string userId) =>
        new(new ClaimsIdentity(new[] { new Claim("sub", userId) }, "TestAuth"));

    private static ClaimsPrincipal Admin(string userId) =>
        new(new ClaimsIdentity(new[] { new Claim("sub", userId), new Claim("role", "superuser") }, "TestAuth"));

    private static Liste List(
        string id,
        string? ownerId = null,
        string? legacyUserId = null,
        IEnumerable<ListeItem>? items = null) =>
        new()
        {
            Id = id,
            Name = id,
            Date = new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc),
            OwnerId = ownerId,
            LegacyUserId = legacyUserId,
            Items = items?.Select(CloneItem).ToList() ?? new List<ListeItem>(),
            DishIds = new List<string>(),
        };

    private static ListeItem Item(string ingredientId, bool isChecked = false) =>
        new()
        {
            IngredientId = ingredientId,
            IngredientName = ingredientId,
            Quantity = 1,
            Unit = "g",
            Aisle = "test",
            Checked = isChecked,
        };

    private static Liste Clone(Liste source) =>
        new()
        {
            Id = source.Id,
            Name = source.Name,
            Date = source.Date,
            OwnerId = source.OwnerId,
            LegacyUserId = source.LegacyUserId,
            DishIds = source.DishIds.ToList(),
            Items = source.Items.Select(CloneItem).ToList(),
        };

    private static ListeItem CloneItem(ListeItem source) =>
        new()
        {
            IngredientId = source.IngredientId,
            IngredientName = source.IngredientName,
            Quantity = source.Quantity,
            Unit = source.Unit,
            Aisle = source.Aisle,
            Checked = source.Checked,
        };

    private static class Assert
    {
        public static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }

        public static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        public static void NotNull(object? value, string message)
        {
            if (value is null) throw new InvalidOperationException(message);
        }

        public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var expectedList = expected.ToList();
            var actualList = actual.ToList();
            if (expectedList.Count != actualList.Count || !expectedList.SequenceEqual(actualList))
            {
                throw new InvalidOperationException(
                    $"Expected sequence [{string.Join(", ", expectedList)}], got [{string.Join(", ", actualList)}].");
            }
        }

        public static async Task<DomainException> ThrowsDomainAsync(Func<Task> action, HttpStatusCode expectedStatus)
        {
            try
            {
                await action();
            }
            catch (DomainException ex)
            {
                Equal(expectedStatus, ex.HttpStatus);
                return ex;
            }

            throw new InvalidOperationException($"Expected {nameof(DomainException)} with status {expectedStatus}.");
        }
    }

    private sealed class InMemoryListeRepository : ListeRepository
    {
        private readonly Dictionary<string, Liste> _items;
        private int _nextId = 1;

        public InMemoryListeRepository(IEnumerable<Liste> items)
            : base(DummyMongo.Database)
        {
            _items = items.ToDictionary(x => x.Id, Clone, StringComparer.Ordinal);
        }

        public Liste? Stored(string id) => _items.TryGetValue(id, out var item) ? Clone(item) : null;

        public override Task<List<Liste>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult(_items.Values.Select(Clone).ToList());

        public override Task<List<Liste>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) =>
            Task.FromResult(_items.Values
                .Where(x => string.Equals(x.EffectiveOwnerId, ownerId, StringComparison.Ordinal))
                .Select(Clone)
                .ToList());

        public override Task<Liste?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(_items.TryGetValue(id, out var item) ? Clone(item) : null);

        public override Task<Liste> CreateAsync(Liste entity, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(entity.Id)) entity.Id = $"generated-{_nextId++}";
            _items[entity.Id] = Clone(entity);
            return Task.FromResult(entity);
        }

        public override Task<bool> UpdateAsync(string id, Liste entity, CancellationToken ct = default)
        {
            if (!_items.ContainsKey(id)) return Task.FromResult(false);
            entity.Id = id;
            _items[id] = Clone(entity);
            return Task.FromResult(true);
        }

        public override Task<bool> DeleteAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(_items.Remove(id));
    }

    private sealed class InMemoryPlatRepository : PlatRepository
    {
        private readonly Dictionary<string, Plat> _items;

        public InMemoryPlatRepository(IEnumerable<Plat> items)
            : base(DummyMongo.Database)
        {
            _items = items.ToDictionary(x => x.Id, x => x, StringComparer.Ordinal);
        }

        public override Task<Plat?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);
    }

    private sealed class InMemoryIngredientRepository : IngredientRepository
    {
        private readonly Dictionary<string, Ingredient> _items;

        public InMemoryIngredientRepository(IEnumerable<Ingredient> items)
            : base(DummyMongo.Database)
        {
            _items = items.ToDictionary(x => x.Id, x => x, StringComparer.Ordinal);
        }

        public override Task<Ingredient?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);
    }

    private static class DummyMongo
    {
        public static readonly IMongoDatabase Database =
            new MongoClient("mongodb://localhost:27017").GetDatabase("ShopListTests");
    }
}
