using Inventory.Application.Abstractions;
using Inventory.Application.Features.Stocks;
using Inventory.Domain.Entities;
using Moq;

namespace Inventory.Application.Tests.Features.Stocks
{
    public class GetStockUseCaseTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldReturnStock_WhenStockExists()
        {
            Guid productId = Guid.NewGuid();
            Guid warehouseId = Guid.NewGuid();
            var stock = new Stock(productId, warehouseId);
            stock.Receive(10);

            var stockRepositoryMock = new Mock<IStockRepository>();
            stockRepositoryMock
                .Setup(repository => repository.GetByProductAndWarehouseAsync(
                    productId, warehouseId, CancellationToken.None))
                .ReturnsAsync(stock);

            var useCase = new GetStockUseCase(stockRepositoryMock.Object);

            var result = await useCase.ExecuteAsync(productId, warehouseId, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(10, result.Quantity);

            stockRepositoryMock.Verify(
                repository => repository.GetByProductAndWarehouseAsync(
                    productId, warehouseId, CancellationToken.None),
                Times.Once());
        }
    }
}
