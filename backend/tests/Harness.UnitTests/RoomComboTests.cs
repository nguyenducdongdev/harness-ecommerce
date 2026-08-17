using Harness.Modules.Catalog.Domain;
using Xunit;

namespace Harness.UnitTests;

/// <summary>Kiểm thử combo phòng (gộp nhiều sản phẩm thành không gian).</summary>
public class RoomComboTests
{
    private static RoomCombo NewCombo() => RoomCombo.Create("Combo Phòng khách", "combo-phong-khach", RoomType.LivingRoom, "Mô tả");

    [Fact]
    public void Create_WithSlug_SetsRoomType()
    {
        var combo = NewCombo();
        Assert.Equal(RoomType.LivingRoom, combo.RoomType);
        Assert.True(combo.IsActive);
        Assert.Empty(combo.Items);
    }

    [Fact]
    public void Create_EmptyName_Throws()
        => Assert.Throws<ArgumentException>(() => RoomCombo.Create(" ", "combo", RoomType.BedRoom, null));

    [Fact]
    public void Create_EmptySlug_Throws()
        => Assert.Throws<ArgumentException>(() => RoomCombo.Create("Tên", "", RoomType.BedRoom, null));

    [Fact]
    public void AddItem_Accumulates()
    {
        var combo = NewCombo();
        combo.AddItem(1);
        combo.AddItem(2, 2);

        Assert.Equal(2, combo.Items.Count);
    }

    [Fact]
    public void AddItem_ZeroQuantity_Throws()
    {
        var combo = NewCombo();
        Assert.Throws<ArgumentException>(() => combo.AddItem(1, 0));
    }
}
