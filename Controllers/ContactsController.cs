using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CSAddressBook.Data;
using CSAddressBook.Models;
using CSAddressBook.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using CSAddressBook.Services.Interfaces;

namespace CSAddressBook.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {

        // Dependency Injection
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IAddressBookService _addressBookService;

        // constructor builds the blueprint and gives you an object, the controller is a constructor
        public ContactsController(ApplicationDbContext context, 
                                  UserManager<AppUser> userManager, 
                                  IImageService imageService,
                                  IAddressBookService addressBookService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _addressBookService = addressBookService;

        }

        // GET: Contacts
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User)!;

            // List has IEnumerable implemented as an interface
            List<Contact> contacts = new List<Contact>();

           
            // Eager Load my data -use .Include() to ask for Categories- categories weren't showing up so we have to ask for it
            // query to get contacts where the app user id equals the user id we have(filters the data), anytime we talk to the database use ToListAsync
            contacts = await _context.Contacts.Where(c => c.AppUserId == userId).Include(c => c.Categories).ToListAsync();

            return View(contacts);
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        public async Task<IActionResult> Create()
        {

            string? userId = _userManager.GetUserId(User);

            // Query and present list of Categories for logged in user
            IEnumerable<Category> categoriesList = await _context.Categories
                                                                 .Where(c => c.AppUserId == userId)
                                                                 .ToListAsync();

            // 3rd parameter is what we want to see in the list - dataTextField
            // 2nd parameter is what I want to get when I select multiple of those names
            ViewData["CategoryList"] = new MultiSelectList(categoriesList, "Id", "Name");

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>()); 
            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,BirthDate,Address1,Address2,City,States,ZipCode,Email,PhoneNumber,Created,ImageFile,AppUserId")] Contact contact, IEnumerable<int> selected)
        {
            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                contact.AppUserId = _userManager.GetUserId(User);
                contact.Created = DateTime.UtcNow;

                if(contact.ImageFile != null)
                {
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType;
                }

                if(contact.BirthDate != null)
                {
                    contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                }
 
                _context.Add(contact);
                await _context.SaveChangesAsync();

                // TODO: Add Service call
                await _addressBookService.AddContactToCategoriesAsync(selected, contact.Id);

                return RedirectToAction(nameof(Index));
            }
            
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                                        .Include(c => c.Categories)
                                        .FirstOrDefaultAsync(c => c.Id == id);

            // Query and present the list of Categories for the logged in user
            string? userId = _userManager.GetUserId(User);

            IEnumerable<Category> categoriesList = await _context.Categories
                                                                 .Where(c => c.AppUserId == userId)
                                                                 .ToListAsync();

            IEnumerable<int> currentCategories = contact!.Categories.Select(c => c.Id);


            ViewData["CategoryList"] = new MultiSelectList(categoriesList, "Id", "Name", currentCategories);
            
            if (contact == null)
            {
                return NotFound();
            }
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,BirthDate,Address1,Address2,City,States,ZipCode,Email,PhoneNumber,Created,ImageData,ImageType,ImageFile,AppUserId")] Contact contact, IEnumerable<int> selected)
        {

            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Reformat created date
                    contact.Created = DateTime.SpecifyKind(contact.Created, DateTimeKind.Utc);

                    // Check if Image was Updated
                    if (contact.ImageFile != null)
                    {
                        contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ImageType = contact.ImageFile.ContentType;
                    }

                    // reformat Birth Date
                    if (contact.BirthDate != null)
                    {
                        contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                    }


                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    // TODO:
                    // Add use of the AddressBookService
                    // DONE!

                    if(selected != null)
                    {
                    // 1. Remove Contact's categories
                    await _addressBookService.RemoveAllContactCategoriesAsync(contact.Id);

                    // 2. Add selected categories to the contact
                    await _addressBookService.AddContactToCategoriesAsync(selected, contact.Id);
                    // "abstraction layer" is the set of services we have ex. _addressBookService
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contacts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contacts'  is null.");
            }
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
          return (_context.Contacts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
