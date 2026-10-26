namespace Inventory.Domain.Test
{
    using Inventory.Domain.Entities;
    using Inventory.Domain.Enums;

    public class StockMovementTest
    {
        [Fact]
        public void Constructor_ShouldCreateMovement_WhenDataIsValid()
        {
            Guid productId = Guid.NewGuid();
            Guid warehouseId = Guid.NewGuid();
            DateTimeOffset before = DateTimeOffset.UtcNow;

            var movement = new StockMovement(productId, warehouseId, StockMovementType.Entry, 10);

            DateTimeOffset after = DateTimeOffset.UtcNow;

            Assert.Equal(productId, movement.ProductId);
            Assert.Equal(warehouseId, movement.WarehouseId);
            Assert.Equal(StockMovementType.Entry, movement.Type);
            Assert.Equal(10, movement.Quantity);
            Assert.NotEqual(Guid.Empty, movement.Id);
            Assert.InRange(movement.CreatedAt, before, after);
        }
    }
}
