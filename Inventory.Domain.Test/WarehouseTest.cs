using Inventory.Domain.Entities;

namespace Inventory.Domain.Test
{
    public class WarehouseTest
    {
        [Fact]
        public void Constructor_ShouldNormalizeWarehouse_WhenInputIsValid()
        {
            string code = " mor-001 ";
            string name = " Almacén Morelia";

            var warehouse = new Warehouse(code, name);
            Assert.Equal("MOR-001", warehouse.Code);
            Assert.Equal("Almacén Morelia", warehouse.Name);
            Assert.NotEqual(Guid.Empty, warehouse.Id);
            Assert.True(warehouse.IsActive);
        }
    }
}
