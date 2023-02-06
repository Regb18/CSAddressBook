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

        public async Task AddContactToCategoriesAsync(IEnumerable<int> categoryIds, int contactId)
        {
            try
            {
                Contact? contact = await _context.Contacts
                                                 .Include(c => c.Categories) // Eager Load
                                                 .FirstOrDefaultAsync(c => c.Id == contactId);

                foreach (int categoryId in categoryIds)
                {
                    Category? category = await _context.Categories.FindAsync(categoryId);

                    if(contact != null && category != null)
                    {
                        // Can use add because we're working with objects
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

        public async Task RemoveAllContactCategoriesAsync(int contactId)
        {
            try
            {
                // c represents an individual contact record in the database
                Contact? contact = await _context.Contacts
                                                 .Include(c => c.Categories)
                                                 .FirstOrDefaultAsync(c => c.Id == contactId);

                // we can do this because we used an ICollection
                contact!.Categories.Clear();
                //
                _context.Update(contact);
                await _context.SaveChangesAsync();

            }
            catch (Exception)
            {
                throw;
            }
        }


        // Category Stuff - Testing
        public async Task AddCategoryToContactsAsync(IEnumerable<int> contacts, int categoryId)
        {
            try
            {
                Category? category = await _context.Categories
                                                 .Include(c => c.Contacts) // Eager Load
                                                 .FirstOrDefaultAsync(c => c.Id == categoryId);

                foreach (int contactId in contacts)
                {
                    Contact? contact = await _context.Contacts.FindAsync(contactId);

                    if (contact != null && category != null)
                    {
                        category.Contacts.Add(contact);
                    }
                }

                await _context.SaveChangesAsync();

            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task RemoveAllCategoryContactsAsync(int categoryId)
        {
            try
            {
                // c represents an individual contact record in the database
                Category? category = await _context.Categories
                                                 .Include(c => c.Contacts)
                                                 .FirstOrDefaultAsync(c => c.Id == categoryId);

                // we can do this because we used an ICollection
                category!.Contacts.Clear();
                //
                _context.Update(category);
                await _context.SaveChangesAsync();

            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
