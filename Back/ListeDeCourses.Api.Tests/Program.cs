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
            ("compatible units are converted before shopping list aggregation", CompatibleUnitsAreConvertedBeforeShoppingListAggregation),
            ("incompatible units stay explicit in shopping list aggregation", IncompatibleUnitsStayExplicitInShoppingListAggregation),
            ("manual items are preserved separately from dish aggregation", ManualItemsArePreservedSeparatelyFromDishAggregation),
            ("shopping list materialization batches repository lookups", ShoppingListMaterializationBatchesRepositoryLookups),
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

    private static async Task CompatibleUnitsAreConvertedBeforeShoppingListAggregation()
    {
        var service = CreateService(
            new InMemoryListeRepository(Array.Empty<Liste>()),
            User("user-a"),
            plats:
            [
                Dish("dish-a", IngredientRef("ingredient-a", 500, "g")),
                Dish("dish-b", IngredientRef("ingredient-a", 1, "kg")),
            ],
            ingredients:
            [
                Ingredient("ingredient-a", "Tomate", "Legumes"),
            ]);

        var result = await service.CreateAsync(new ListeCreateDto
        {
            Name = "courses",
            DishIds = new List<string> { "dish-a", "dish-b" },
            Items = new List<ListeItemDto>(),
        });

        var item = result.Items.Single();
        Assert.Equal(1500d, item.Quantity);
        Assert.Equal("g", item.Unit);
        Assert.Equal(1, item.Quantities?.Count ?? 0);
        Assert.Equal(1500d, item.Quantities![0].Quantity);
        Assert.Equal("g", item.Quantities[0].Unit);
    }

    private static async Task IncompatibleUnitsStayExplicitInShoppingListAggregation()
    {
        var service = CreateService(
            new InMemoryListeRepository(Array.Empty<Liste>()),
            User("user-a"),
            plats:
            [
                Dish("dish-a", IngredientRef("ingredient-a", 2, UnitCatalog.PieceUnit)),
                Dish("dish-b", IngredientRef("ingredient-a", 300, "g")),
            ],
            ingredients:
            [
                Ingredient("ingredient-a", "Tomate", "Legumes"),
            ]);

        var result = await service.CreateAsync(new ListeCreateDto
        {
            Name = "courses",
            DishIds = new List<string> { "dish-a", "dish-b" },
            Items = new List<ListeItemDto>(),
        });

        var item = result.Items.Single();
        Assert.Equal<double?>(null, item.Quantity);
        Assert.Equal<string?>(null, item.Unit);
        var quantities = item.Quantities ?? new List<ListeItemQuantityDto>();
        Assert.True(quantities.Count == 2, "Expected one explicit quantity per incompatible unit family.");
        Assert.True(quantities.Any(q => q.Quantity == 300d && q.Unit == "g"), "Expected the mass quantity to remain explicit.");
        Assert.True(quantities.Any(q => q.Quantity == 2d && q.Unit == UnitCatalog.PieceUnit), "Expected the piece quantity to remain explicit.");
    }

    private static async Task ManualItemsArePreservedSeparatelyFromDishAggregation()
    {
        var lists = new InMemoryListeRepository(Array.Empty<Liste>());
        var service = CreateService(
            lists,
            User("user-a"),
            plats:
            [
                Dish("dish-a", IngredientRef("ingredient-a", 300, "g")),
            ],
            ingredients:
            [
                Ingredient("ingredient-a", "Tomate", "Legumes"),
            ]);

        var result = await service.CreateAsync(new ListeCreateDto
        {
            Name = "courses",
            DishIds = new List<string> { "dish-a" },
            Items =
            [
                new ListeItemDto
                {
                    IngredientId = "ingredient-a",
                    IngredientName = "Tomate",
                    Quantity = 200,
                    Unit = "g",
                    Aisle = "Legumes",
                    Checked = false,
                }
            ],
        });

        var item = result.Items.Single();
        Assert.Equal(500d, item.Quantity);
        Assert.Equal("g", item.Unit);
        Assert.Equal(1, result.ManualItems.Count);
        Assert.Equal(200d, result.ManualItems[0].Quantity);
        Assert.Equal("g", result.ManualItems[0].Unit);

        var stored = lists.Stored(result.Id);
        Assert.NotNull(stored, "Expected the created shopping list to be stored.");
        Assert.Equal(1, stored!.ManualItems.Count);
        Assert.Equal(200d, stored.ManualItems[0].Quantity);
        Assert.Equal("g", stored.ManualItems[0].Unit);
    }

    private static async Task ShoppingListMaterializationBatchesRepositoryLookups()
    {
        var plats = new CountingPlatRepository(new[]
        {
            Dish("dish-1", IngredientRef("ingredient-a", 1, "g"), IngredientRef("ingredient-b", 2, "g"), IngredientRef("ingredient-c", 3, "g")),
            Dish("dish-2", IngredientRef("ingredient-b", 1, "g"), IngredientRef("ingredient-d", 4, "g")),
            Dish("dish-3", IngredientRef("ingredient-c", 2, "g"), IngredientRef("ingredient-e", 5, "g")),
        });
        var ingredients = new CountingIngredientRepository(new[]
        {
            Ingredient("ingredient-a"),
            Ingredient("ingredient-b"),
            Ingredient("ingredient-c"),
            Ingredient("ingredient-d"),
            Ingredient("ingredient-e"),
            Ingredient("ingredient-f"),
        });

        var context = new DefaultHttpContext { User = User("user-a") };
        var service = new ListeService(
            new InMemoryListeRepository(Array.Empty<Liste>()),
            plats,
            ingredients,
            new HttpContextAccessor { HttpContext = context });

        var result = await service.CreateAsync(new ListeCreateDto
        {
            Name = "batch-check",
            Date = new DateTime(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc),
            DishIds = new List<string> { "dish-1", "dish-2", "dish-3" },
            Items =
            [
                new ListeItemDto { IngredientId = "ingredient-b", IngredientName = "ingredient-b", Quantity = 1, Unit = "g" },
                new ListeItemDto { IngredientId = "ingredient-f", IngredientName = "ingredient-f", Quantity = 2, Unit = "g" },
            ],
        });

        Assert.Equal(0, plats.GetByIdCalls);
        Assert.Equal(1, plats.GetByIdsCalls);
        Assert.Equal(0, ingredients.GetByIdCalls);
        Assert.Equal(2, ingredients.GetByIdsCalls);
        Assert.Equal(6, result.Items.Count);
    }

    private static ListeService CreateService(IEnumerable<Liste> seed, ClaimsPrincipal? principal) =>
        CreateService(new InMemoryListeRepository(seed), principal);

    private static ListeService CreateService(InMemoryListeRepository lists, ClaimsPrincipal? principal)
        => CreateService(lists, principal, plats: null, ingredients: null);

    private static ListeService CreateService(
        InMemoryListeRepository lists,
        ClaimsPrincipal? principal,
        IEnumerable<Plat>? plats,
        IEnumerable<Ingredient>? ingredients)
    {
        var context = new DefaultHttpContext();
        if (principal is not null) context.User = principal;

        return new ListeService(
            lists,
            new InMemoryPlatRepository(plats ?? Array.Empty<Plat>()),
            new InMemoryIngredientRepository(ingredients ?? Array.Empty<Ingredient>()),
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
            ManualItems = new List<ListeItem>(),
            DishIds = new List<string>(),
        };

    private static ListeItem Item(string ingredientId, bool isChecked = false) =>
        new()
        {
            IngredientId = ingredientId,
            IngredientName = ingredientId,
            Quantity = 1,
            Quantities =
            [
                new ListeItemQuantity
                {
                    Quantity = 1,
                    Unit = "g"
                }
            ],
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
            ManualItems = source.ManualItems.Select(CloneItem).ToList(),
        };

    private static ListeItem CloneItem(ListeItem source) =>
        new()
        {
            IngredientId = source.IngredientId,
            IngredientName = source.IngredientName,
            Quantity = source.Quantity,
            Quantities = source.Quantities.Select(CloneQuantity).ToList(),
            Unit = source.Unit,
            Aisle = source.Aisle,
            Checked = source.Checked,
        };

    private static ListeItemQuantity CloneQuantity(ListeItemQuantity source) =>
        new()
        {
            Quantity = source.Quantity,
            Unit = source.Unit,
        };

    private static Plat Dish(string id, params PlatIngredient[] ingredients) =>
        new()
        {
            Id = id,
            Name = id,
            Ingredients = ingredients.ToList(),
        };

    private static PlatIngredient IngredientRef(string ingredientId, double quantity, string unit) =>
        new()
        {
            IngredientId = ingredientId,
            Quantity = quantity,
            Unit = unit,
        };

    private static Ingredient Ingredient(string id, string name, string? aisle) =>
        new()
        {
            Id = id,
            Name = name,
            Aisle = aisle,
        };

    private static Ingredient Ingredient(string id) => Ingredient(id, id, "test");

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

    private class InMemoryPlatRepository : PlatRepository
    {
        private readonly Dictionary<string, Plat> _items;

        public InMemoryPlatRepository(IEnumerable<Plat> items)
            : base(DummyMongo.Database)
        {
            _items = items.ToDictionary(x => x.Id, x => x, StringComparer.Ordinal);
        }

        public override Task<Plat?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

        public override Task<List<Plat>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default) =>
            Task.FromResult(ids
                .Where(id => _items.ContainsKey(id))
                .Select(id => _items[id])
                .ToList());
    }

    private class InMemoryIngredientRepository : IngredientRepository
    {
        private readonly Dictionary<string, Ingredient> _items;

        public InMemoryIngredientRepository(IEnumerable<Ingredient> items)
            : base(DummyMongo.Database)
        {
            _items = items.ToDictionary(x => x.Id, x => x, StringComparer.Ordinal);
        }

        public override Task<Ingredient?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

        public override Task<List<Ingredient>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default) =>
            Task.FromResult(ids
                .Where(id => _items.ContainsKey(id))
                .Select(id => _items[id])
                .ToList());
    }

    private sealed class CountingPlatRepository : InMemoryPlatRepository
    {
        public int GetByIdCalls { get; private set; }
        public int GetByIdsCalls { get; private set; }

        public CountingPlatRepository(IEnumerable<Plat> items)
            : base(items) { }

        public override async Task<Plat?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            GetByIdCalls++;
            return await base.GetByIdAsync(id, ct);
        }

        public override async Task<List<Plat>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
        {
            GetByIdsCalls++;
            return await base.GetByIdsAsync(ids, ct);
        }
    }

    private sealed class CountingIngredientRepository : InMemoryIngredientRepository
    {
        public int GetByIdCalls { get; private set; }
        public int GetByIdsCalls { get; private set; }

        public CountingIngredientRepository(IEnumerable<Ingredient> items)
            : base(items) { }

        public override async Task<Ingredient?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            GetByIdCalls++;
            return await base.GetByIdAsync(id, ct);
        }

        public override async Task<List<Ingredient>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
        {
            GetByIdsCalls++;
            return await base.GetByIdsAsync(ids, ct);
        }
    }

    private static class DummyMongo
    {
        public static readonly IMongoDatabase Database =
            new MongoClient("mongodb://localhost:27017").GetDatabase("ShopListTests");
    }
}
