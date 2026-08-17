using Harness.Modules.Catalog.Domain;
using Xunit;

namespace Harness.UnitTests;

public class ProductTests
{
    private static Product NewProduct(decimal price = 10_000_000, decimal? salePrice = null) =>
        Product.Create("Sofa góc Test", "sofa-goc-test", "SKU-TEST", 1, 1,
            price, salePrice, 24, null, null);

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var product = NewProduct();

        Assert.Equal("Sofa góc Test", product.Name);
        Assert.Equal(price: 10_000_000, actual: product.Price);
        Assert.True(product.IsActive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidPrice_Throws(decimal price)
        => Assert.Throws<ArgumentException>(() => NewProduct(price));

    [Fact]
    public void Create_WithSalePriceGreaterOrEqualPrice_Throws()
        => Assert.Throws<ArgumentException>(() => NewProduct(price: 5_000_000, salePrice: 5_000_000));

    [Fact]
    public void AddVariant_DuplicateSku_Throws()
    {
        var product = NewProduct();
        product.AddVariant(ProductVariant.Create(product.Id, "SKU-V1", "100x200x20cm", 100, 200, 20));

        Assert.Throws<InvalidOperationException>(() =>
            product.AddVariant(ProductVariant.Create(product.Id, "SKU-V1", "100x200x20cm", 100, 200, 20)));
    }

    [Fact]
    public void Variant_EffectivePrice_UsesOverride_WhenPresent()
    {
        var product = NewProduct();
        var variant = ProductVariant.Create(product.Id, "SKU-V2", "180x200x25cm", 180, 200, 25, priceOverride: 12_000_000);

        Assert.Equal(12_000_000, variant.GetEffectivePrice(product.Price));
    }

    [Fact]
    public void Variant_InvalidSize_Throws()
        => Assert.Throws<ArgumentException>(
            () => ProductVariant.Create(1, "SKU-V3", "0x200x20", 0, 200, 20));

    [Fact]
    public void UpdatePrice_Valid_ChangesPrice()
    {
        var product = NewProduct();
        product.UpdatePrice(9_000_000, 8_000_000);

        Assert.Equal(9_000_000, product.Price);
        Assert.Equal(8_000_000, product.SalePrice);
    }
}
