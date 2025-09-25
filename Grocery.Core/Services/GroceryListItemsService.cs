using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;

namespace Grocery.Core.Services
{
    public class GroceryListItemsService : IGroceryListItemsService
    {
        private readonly IGroceryListItemsRepository _groceriesRepository;
        private readonly IProductRepository _productRepository;

        public GroceryListItemsService(IGroceryListItemsRepository groceriesRepository, IProductRepository productRepository)
        {
            _groceriesRepository = groceriesRepository;
            _productRepository = productRepository;
        }

        public List<GroceryListItem> GetAll()
        {
            List<GroceryListItem> groceryListItems = _groceriesRepository.GetAll();
            FillService(groceryListItems);
            return groceryListItems;
        }

        public List<GroceryListItem> GetAllOnGroceryListId(int groceryListId)
        {
            List<GroceryListItem> groceryListItems = _groceriesRepository.GetAll().Where(g => g.GroceryListId == groceryListId).ToList();
            FillService(groceryListItems);
            return groceryListItems;
        }

        public GroceryListItem Add(GroceryListItem item)
        {
            return _groceriesRepository.Add(item);
        }

        // UC09: Implementatie van Delete methode
        public GroceryListItem? Delete(GroceryListItem item)
        {
            return _groceriesRepository.Delete(item);
        }

        // UC09: Implementatie van Delete methode met ID
        public GroceryListItem? Delete(int id)
        {
            var item = _groceriesRepository.Get(id);
            if (item != null)
            {
                return _groceriesRepository.Delete(item);
            }
            return null;
        }

        // UC09: Implementatie van Get methode
        public GroceryListItem? Get(int id)
        {
            var item = _groceriesRepository.Get(id);
            if (item != null)
            {
                // Fill product information
                item.Product = _productRepository.Get(item.ProductId) ?? new(0, "", 0);
            }
            return item;
        }

        // UC09: Implementatie van Update methode
        public GroceryListItem? Update(GroceryListItem item)
        {
            return _groceriesRepository.Update(item);
        }

        private void FillService(List<GroceryListItem> groceryListItems)
        {
            foreach (GroceryListItem g in groceryListItems)
            {
                g.Product = _productRepository.Get(g.ProductId) ?? new(0, "", 0);
            }
        }
    }
}