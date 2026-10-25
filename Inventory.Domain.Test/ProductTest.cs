using Inventory.Domain.Entities;

namespace Inventory.Domain.Test
{
    public class ProductTest
    {
        [Theory]
        [InlineData(3)]
        [InlineData(40)]
        public void Constructor_ShouldAcceptSku_WhenLengthIsWithinLimits(int length)
        {
            string sku = new string('A', length);
            var product = new Product(sku, "Laptop");

            Assert.Equal(sku, product.Sku);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(41)]
        public void Constructor_ShouldRejectSku_WhenLengthIsOutsideLimits(int length)
        {
            string sku = new string('A', length);

            Assert.Throws<ArgumentException>(() => new Product(sku, "Laptop"));
        }

        [Fact]
        public void Constructor_ShouldNormalizedSku_WhenInputIsvalid()
        {
            string sku = " lap-001 ";
            string name = "Laptop";

            var  product = new Product(sku, name);
            Assert.Equal("LAP-001", product.Sku);
            Assert.Equal(name, product.Name);
        }

        [Fact]
        public void UpdateName_ShouldPreserveOriginalName_WhenNewNameIsInvalid()
        {
            string sku = "lap-002";
            string name = "Dell Laptop";

            var product = new Product(sku, name);

            Assert.Throws<ArgumentException>(() => product.UpdateName(""));
            Assert.Equal("Dell Laptop", product.Name);

        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void UpdateName_ShouldRejectInvalidNames(string invalidName)
        {
            string sku = "lap-002";
            string name = "Dell Laptop";

            var product = new Product(sku, name);
            Assert.Throws<ArgumentException>(() => product.UpdateName(invalidName));
            Assert.Equal("Dell Laptop", product.Name);
        }

        [Fact]
        public void UpdateName_ShouldRejectName_WhenLengthExceeds120()
        {
            string sku = "lap-002";
            string name = "Laptop";

            var product = new Product(sku, name);
            string longName = new string('A', 121);
            Assert.Throws<ArgumentException>(() => product.UpdateName(longName));
            Assert.Equal("Laptop", product.Name);
        }
        [Fact]
        public void UpdateName_ShouldAcceptName_WhenLengthIsExactly120()
        {
            string sku = "lap-002";
            string name = "Laptop";

            var product = new Product(sku, name);
            string longName = new string('A', 120);
            product.UpdateName(longName);
            Assert.Equal(longName, product.Name);
        }
    }
}
