using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        private readonly IFileSaverService _fileSaverService;

        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);
        [ObservableProperty]
        string myMessage;

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService, IFileSaverService fileSaverService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            _fileSaverService = fileSaverService;
            Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id)) MyGroceryListItems.Add(item);
            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear();
            foreach (Product p in _productService.GetAll())
                if (MyGroceryListItems.FirstOrDefault(g => g.ProductId == p.Id) == null && p.Stock > 0)
                    AvailableProducts.Add(p);
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }

        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product == null) return;
            GroceryListItem item = new(0, GroceryList.Id, product.Id, 1);
            _groceryListItemsService.Add(item);
            product.Stock--;
            _productService.Update(product);
            AvailableProducts.Remove(product);
            OnGroceryListChanged(GroceryList);
        }

        [RelayCommand]
        public async Task ShareGroceryList(CancellationToken cancellationToken)
        {
            if (GroceryList == null || MyGroceryListItems == null) return;
            string jsonString = JsonSerializer.Serialize(MyGroceryListItems);
            try
            {
                await _fileSaverService.SaveFileAsync("Boodschappen.json", jsonString, cancellationToken);
                await Toast.Make("Boodschappenlijst is opgeslagen.").Show(cancellationToken);
            }
            catch (Exception ex)
            {
                await Toast.Make($"Opslaan mislukt: {ex.Message}").Show(cancellationToken);
            }
        }

        [RelayCommand]
        public void Search(string searchTerm)
        {
            GetAvailableProducts();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var filteredProducts = AvailableProducts
                    .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                AvailableProducts.Clear();
                foreach (var product in filteredProducts)
                {
                    AvailableProducts.Add(product);
                }
            }
        }

        // UC09: Nieuwe commands voor wijzigen hoeveelheid
        [RelayCommand]
        public void IncreaseAmount(GroceryListItem item)
        {
            if (item == null) return;

            try
            {
                // Verhoog de hoeveelheid
                item.Amount++;

                // Update in de database
                _groceryListItemsService.Update(item);

                // Update alleen de UI, geen volledige herlaad
                var existingItem = MyGroceryListItems.FirstOrDefault(x => x.Id == item.Id);
                if (existingItem != null)
                {
                    existingItem.Amount = item.Amount;
                }
            }
            catch (Exception ex)
            {
                // Log error en reset
                MyMessage = $"Fout bij verhogen: {ex.Message}";
                item.Amount--; // Revert change
            }
        }

        [RelayCommand]
        public void DecreaseAmount(GroceryListItem item)
        {
            if (item == null) return;

            try
            {
                if (item.Amount > 1)
                {
                    // Verlaag de hoeveelheid (maar niet onder 1)
                    item.Amount--;
                    _groceryListItemsService.Update(item);

                    // Update alleen de UI
                    var existingItem = MyGroceryListItems.FirstOrDefault(x => x.Id == item.Id);
                    if (existingItem != null)
                    {
                        existingItem.Amount = item.Amount;
                    }
                }
                else
                {
                    // Als hoeveelheid 1 is, verwijder het item helemaal
                    RemoveItem(item);
                }
            }
            catch (Exception ex)
            {
                MyMessage = $"Fout bij verlagen: {ex.Message}";
                item.Amount++; // Revert change if it was decreased
            }
        }

        [RelayCommand]
        public void RemoveItem(GroceryListItem item)
        {
            if (item == null) return;

            try
            {
                // Verhoog stock van het product weer VOOR het verwijderen
                var product = _productService.Get(item.ProductId);
                if (product != null)
                {
                    product.Stock++;
                    _productService.Update(product);
                }

                // Verwijder item uit database
                _groceryListItemsService.Delete(item);

                // Verwijder uit UI collectie
                MyGroceryListItems.Remove(item);

                // Update beschikbare producten
                GetAvailableProducts();
            }
            catch (Exception ex)
            {
                MyMessage = $"Fout bij verwijderen: {ex.Message}";
                // Revert stock change if delete failed
                var product = _productService.Get(item.ProductId);
                if (product != null)
                {
                    product.Stock--;
                    _productService.Update(product);
                }
            }
        }
    }
}