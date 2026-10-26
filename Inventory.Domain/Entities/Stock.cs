namespace Inventory.Domain.Entities
{
    public class Stock
    {
        public Guid ProductId { get; private set; }
        public Guid WarehouseId { get; private set; }
        public int Quantity { get; private set; }

        public Stock(Guid productId, Guid warehouseId)
        {
            if (productId == Guid.Empty)
            {
                throw new ArgumentException(
                    "ProductId caanot be empty.",
                    nameof(productId));
            }

            if (warehouseId == Guid.Empty)
            {
                throw new ArgumentException(
                    "WarehouseId cannot be empty.",
                    nameof(warehouseId));
            }
            ProductId = productId;
            WarehouseId = warehouseId;
            Quantity = 0;
        }

        public void Withdraw(int quantity)
        {   
            ValidateQuantity(quantity);
            ValidateStock(quantity);
            Quantity -= quantity;
        }

        public void Receive(int quantity)
        {
            ValidateQuantity(quantity);
            try
            {
                checked
                {
                    Quantity += quantity;
                }
            }
            catch (OverflowException ex)
            {
                throw new ArgumentException(
                    "The quantity exceeds the maximum allowed value."
                    , nameof(quantity),
                    ex);
            }
        }

        private void ValidateQuantity(int quantity)
        {
            if(quantity <= 0)
            {
                throw new ArgumentException(
                    "Quantity must be higher than 0.",
                    nameof(quantity));
            }
        }

        private void ValidateStock(int quantity)
        {
            if (quantity > Quantity)
            {
                throw new ArgumentException(
                    "Not enough items.",
                    nameof(quantity));
            }
        }
    }
}
