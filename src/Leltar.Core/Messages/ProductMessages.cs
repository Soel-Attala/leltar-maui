using Leltar.Core.Models;

namespace Leltar.Core.Messages;

// Sikeres adatbázis-írás után küldött üzenetek, hogy a lista teljes újratöltés nélkül frissüljön

// Termék hozzáadva
public sealed record ProductAddedMessage(Product Product);

// Termék módosítva
public sealed record ProductUpdatedMessage(Product Product);

// Termék törölve
public sealed record ProductDeletedMessage(int ProductId);
