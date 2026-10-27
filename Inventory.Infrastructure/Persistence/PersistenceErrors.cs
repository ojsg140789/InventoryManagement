using Inventory.Application.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

internal static class PersistenceErrors
{
    public static Exception Translate(DbUpdateException exception) => exception switch
    {
        DbUpdateConcurrencyException => new InventoryConflictException(
            "Stock changed concurrently. Read current stock and submit a new request.", exception),
        { InnerException: SqlException { Number: 1205 } } => new InventoryConflictException(
            "Transaction conflicted with another operation. Read current state before retrying.", exception),
        { InnerException: SqlException { Number: 2601 or 2627 } } => new InventoryConflictException(
            "A unique value already exists or stock was initialized concurrently. Read current state before retrying.", exception),
        { InnerException: SqlException { Number: 547 } } => new InventoryConflictException(
            "The operation conflicts with a data integrity constraint.", exception),
        _ => exception
    };
}
