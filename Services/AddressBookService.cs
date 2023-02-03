using CSAddressBook.Data;
using CSAddressBook.Models;
using CSAddressBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CSAddressBook.Services
{
    public class AddressBookService : IAddressBookService
    {
        private readonly ApplicationDbContext _context;

        // constructor is the same name as the class
        public AddressBookService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddContactToCategoriesAsync(IEnumerable<int> categories, int contactId)
        {
            try
            {
                Contact? contact = await _context.Contacts
                                                 .Include(c => c.Categories) // Eager Load
                                                 .FirstOrDefaultAsync(c => c.Id == contactId);

                foreach (int categoryId in categories)
                {
                    Category? category = await _context.Categories.FindAsync(categoryId);

                    if(contact != null && category != null)
                    {
                        contact.Categories.Add(category);
                    } 
                }

                await _context.SaveChangesAsync();

            }
            catch (Exception)
            {

                throw;
            }
            
        }

        public Task AddContactToCategoryAsync(int categoryId, int contactId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Category>> GetAppUserCategoriesAsync(string appUserId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsContactInCategory(int categoryId, int contactId)
        {

            try
            {
                // First or default finds the first Id that matches that contactId
                // or it will give us back an empty instance of a contact as if contact = new contact
                // To find the one contact based on their primary key compared to the contactId I'm passing in
                // LINQ statement to query the database to filter through and find something
                Contact? contact = await _context.Contacts
                                                 .Include(c => c.Categories) // Eager Load
                                                 .FirstOrDefaultAsync(c => c.Id == contactId);

                bool isInCategory = contact!.Categories.Select(c => c.Id).Contains(categoryId);

                return isInCategory;

            }
            catch (Exception)
            {

                throw;
            }
        }

        public Task RemoveAllContactCategoriesAsync(int contactId)
        {
            throw new NotImplementedException();
        }
    }
}
