namespace Inventory.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; private set; }
        public string Sku { get; private set; }
        public string Name { get; private set; }
        public bool IsActive { get; private set; }

        public Product(string sku, string name)
        {
            Sku = ValidateSku(sku);
            Name = ValidateName(name);

            Id = Guid.NewGuid();
            IsActive = true;
        }

        public void UpdateName(string name)
        {
            Name = ValidateName(name);
        }

        private static string ValidateSku(string? sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                throw new ArgumentException(
                    "SKU cannot be empty.",
                    nameof(sku));
            }

            string normalizedSku = sku.Trim().ToUpperInvariant();

            if (normalizedSku.Length < 3 || normalizedSku.Length > 40)
            {
                throw new ArgumentException(
                    "SKU must contain between 3 and 40 characters.",
                    nameof(sku));
            }

            return normalizedSku;
        }

        private static string ValidateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Name cannot be empty.",
                    nameof(name));
            }

            string normalizedName = name.Trim();

            if (normalizedName.Length > 120)
            {
                throw new ArgumentException(
                    "Name cannot exceed 120 characters.",
                    nameof(name));
            }

            return normalizedName;
        }
    }
}