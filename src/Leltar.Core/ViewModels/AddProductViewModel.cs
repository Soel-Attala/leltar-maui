using CommunityToolkit.Mvvm.Messaging;
using Leltar.Core.Data;
using Leltar.Core.Messages;
using Leltar.Core.Models;
using Leltar.Core.Services;
using Leltar.Core.Validation;

namespace Leltar.Core.ViewModels;

// Új termék felvétele
public sealed class AddProductViewModel(
    IProductRepository repository,
    INavigationService navigation,
    IDialogService dialogs,
    IMessenger messenger)
    : ProductFormViewModel(repository, navigation, dialogs, messenger)
{
    public override string Title => "New product"; // Új termék

    protected override async Task PersistAsync(ProductInput input)
    {
        var product = new Product
        {
            Name = input.Name,
            Barcode = input.Barcode,
            Price = input.Price,
            Quantity = input.Quantity,
        };

        await Repository.AddAsync(product);
        Messenger.Send(new ProductAddedMessage(product));
    }
}
