namespace Inventory.Application.Exceptions;

public sealed class ResourceNotFoundException(string message) : Exception(message);
public sealed class InventoryConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
