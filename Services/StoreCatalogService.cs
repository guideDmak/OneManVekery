using OneManVekery.ViewModel;

namespace OneManVekery.Services;

public interface IStoreCatalogService
{
    IReadOnlyList<ProductCardViewModel> GetProducts();

    ProductCardViewModel? GetProductById(string productId);
}

public sealed class InMemoryStoreCatalogService : IStoreCatalogService
{
    private readonly IReadOnlyList<ProductCardViewModel> _products =
    [
        new ProductCardViewModel
        {
            ProductId = "rose-macaron-box",
            Name = "Rose Macaron Box",
            Category = "Macaron",
            Description = "Rose and vanilla macarons for soft pink gift sets",
            Price = 120,
            OriginalPrice = 140,
            Badge = "-15%",
            ThemeKey = "macaron",
            ImagePath = "/images/theme-macaron.svg"
        },
        new ProductCardViewModel
        {
            ProductId = "strawberry-shortcake",
            Name = "Strawberry Shortcake",
            Category = "Cake",
            Description = "Fresh cream cake with soft sponge and strawberry topping",
            Price = 145,
            OriginalPrice = 165,
            Badge = "New",
            ThemeKey = "cake",
            ImagePath = "/images/theme-cake.svg"
        },
        new ProductCardViewModel
        {
            ProductId = "vanilla-choux-cream",
            Name = "Vanilla Choux Cream",
            Category = "Choux Cream",
            Description = "Light pastry shell with smooth vanilla custard filling",
            Price = 55,
            ThemeKey = "cream",
            ImagePath = "/images/theme-cream.svg"
        },
        new ProductCardViewModel
        {
            ProductId = "butter-croissant",
            Name = "Butter Croissant",
            Category = "Bakery",
            Description = "Flaky layers with rich butter aroma from the morning batch",
            Price = 69,
            Badge = "Sold Out",
            ThemeKey = "gold",
            ImagePath = "/images/theme-gold.svg",
            IsSoldOut = true
        },
        new ProductCardViewModel
        {
            ProductId = "blueberry-cheesecake",
            Name = "Blueberry Cheesecake",
            Category = "Cake",
            Description = "Creamy cheesecake finished with blueberry glaze",
            Price = 159,
            Badge = "New",
            ThemeKey = "berry",
            ImagePath = "/images/theme-berry.svg"
        },
        new ProductCardViewModel
        {
            ProductId = "mini-eclair-set",
            Name = "Mini Eclair Set",
            Category = "Bakery",
            Description = "Small eclair box for afternoon sharing and coffee time",
            Price = 89,
            OriginalPrice = 110,
            Badge = "-20%",
            ThemeKey = "cream",
            ImagePath = "/images/theme-cream.svg"
        },
        new ProductCardViewModel
        {
            ProductId = "milk-cloud-roll",
            Name = "Milk Cloud Roll",
            Category = "Cake",
            Description = "Japanese style roll cake with soft milk whipped cream",
            Price = 135,
            ThemeKey = "milk",
            ImagePath = "/images/theme-milk.svg"
        },
        new ProductCardViewModel
        {
            ProductId = "cherry-tart-slice",
            Name = "Cherry Tart Slice",
            Category = "Bakery",
            Description = "Buttery tart shell with cherry compote and almond cream",
            Price = 95,
            ThemeKey = "berry",
            ImagePath = "/images/theme-berry.svg"
        }
    ];

    public IReadOnlyList<ProductCardViewModel> GetProducts()
    {
        return _products.Select(Clone).ToList();
    }

    public ProductCardViewModel? GetProductById(string productId)
    {
        var product = _products.FirstOrDefault(item => item.ProductId == productId);
        return product is null ? null : Clone(product);
    }

    private static ProductCardViewModel Clone(ProductCardViewModel product)
    {
        return new ProductCardViewModel
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Category = product.Category,
            Description = product.Description,
            Price = product.Price,
            OriginalPrice = product.OriginalPrice,
            Badge = product.Badge,
            ThemeKey = product.ThemeKey,
            ImagePath = product.ImagePath,
            IsSoldOut = product.IsSoldOut
        };
    }
}
