using Inventory.Domain.Entities;

namespace Inventory.Domain.Test
{
    public class StockTest
    {
        [Fact]
        public void Receive_ShouldPreserveQuantity_WhenOverflowOccurs()
        {
            Guid productId = Guid.NewGuid();
            Guid warehouseId = Guid.NewGuid();

            var stock = new Stock(productId, warehouseId);

            stock.Receive(int.MaxValue);
            Assert.Equal(int.MaxValue, stock.Quantity);

            Assert.Throws<ArgumentException>(() => stock.Receive(1));
            Assert.Equal(int.MaxValue, stock.Quantity);
        }

        [Fact]
        public void Stock_ShouldUpdateQuantity_WhenReceivingAndWithdrawing()
        {
            Guid productId = Guid.NewGuid();
            Guid warehouseId = Guid.NewGuid();

            var stock = new Stock(productId, warehouseId);
            Assert.Equal(0, stock.Quantity);

            stock.Receive(10);
            Assert.Equal(10, stock.Quantity);

            stock.Withdraw(4);
            Assert.Equal(6, stock.Quantity);
        }

        [Fact]
        public void Withdraw_ShouldPreserveQuantity_WhenStockIsInsufficient()
        {
            Guid productId = Guid.NewGuid();
            Guid warehouseId = Guid.NewGuid();

            var stock  = new Stock(productId, warehouseId);

            stock.Receive(10);
            Assert.Throws<ArgumentException>(() => stock.Withdraw(11));
            Assert.Equal(10, stock.Quantity);
        }
    }
}
