using CSAddressBook.Models;
using Microsoft.EntityFrameworkCore;

namespace CSAddressBook.Services.Interfaces
{
    public interface IAddressBookService
    {
        // no return on these two Tasks
        public Task AddContactToCategoryAsync(int categoryId, int contactId);
        public Task AddContactToCategoriesAsync(IEnumerable<int> categories, int contactId);

        // Returns the IEnumerable<Category>, appUserId is a string because that's what microsoft makes for IdentityUser
        public Task<IEnumerable<Category>> GetAppUserCategoriesAsync(string appUserId);

        public Task<bool> IsContactInCategory(int categoryId, int contactId);

        public Task RemoveAllContactCategoriesAsync(int contactId);

        // Category Stuff - Testing
        public Task AddCategoryToContactsAsync(IEnumerable<int> contacts, int categoryId);

        public Task RemoveAllCategoryContactsAsync(int categoryId);
    }
}
