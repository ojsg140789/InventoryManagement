namespace Inventory.Domain.Entities
{
    public class Warehouse
    {
        public Guid Id { get; private set; }
        public string Code { get; private set; }
        public string Name { get; private set; }
        public bool IsActive { get; private set; }

        public Warehouse(string code, string name)
        {
            Code = ValidateCode(code);
            Name = ValidateName(name);

            Id = Guid.NewGuid();
            IsActive = true;
        }

        private static string ValidateCode(string code)
        {
            if(string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException(
                    "Code cannot be empty."
                    ,nameof(code));
            }

            string normalizedCode = code.Trim().ToUpperInvariant();

            if(normalizedCode.Length < 3 || normalizedCode.Length > 20)
            {
                throw new ArgumentException(
                    "Code must contain between 3 and 20 characters.",
                    nameof(code));
            }
            return normalizedCode;
        }

        private static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Name cannot be empty."
                    , nameof(name));
            }

            string normalizedName = name.Trim();

            if (normalizedName.Length > 100)
            {
                throw new ArgumentException(
                    "Name cannot exceed 100 characters.",
                    nameof(name));
            }
            return normalizedName;
        }
    }
}
