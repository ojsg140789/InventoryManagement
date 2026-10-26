using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities
{
    public class StockMovement
    {
        public Guid Id { get; private set; }
        public Guid ProductId { get; private set; }
        public Guid WarehouseId { get; private set; }
        public StockMovementType Type { get; private set; }
        public int Quantity { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public StockMovement(Guid productId, Guid warehouseId, StockMovementType type, int quantity)
        {
            if(productId == Guid.Empty)
            {
                throw new ArgumentException(
                    "ProductId cannot be empty",
                    nameof(productId));
            }

            if (warehouseId == Guid.Empty)
            {
                throw new ArgumentException(
                    "WarehouseId cannot be empty",
                    nameof(warehouseId));
            }

            ValidateQuantity(quantity);

            if(!Enum.IsDefined(typeof(StockMovementType), type))
            {
                throw new ArgumentException(
                    "Type is not a valid type",
                    nameof(type));
            }

            Id = Guid.NewGuid();
            ProductId = productId;
            WarehouseId = warehouseId;
            Type = type;
            Quantity = quantity;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        private static void ValidateQuantity(int quantity)
        {
            if(quantity <= 0)
            {
                throw new ArgumentException(
                    "Quantity must be higher than 0",
                    nameof(quantity));
            }
        }

    }
}
