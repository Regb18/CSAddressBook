﻿using System;
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
using CSAddressBook.Models.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using CSAddressBook.Services;

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
        private readonly IEmailSender _emailService;

        // constructor builds the blueprint and gives you an object, the controller is a constructor
        public ContactsController(ApplicationDbContext context, 
                                  UserManager<AppUser> userManager, 
                                  IImageService imageService,
                                  IAddressBookService addressBookService,
                                  IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _addressBookService = addressBookService;
            _emailService = emailService;

        }

        // GET: Contacts
                                // Declaration of an optional parameter in a method because we set it equal to null
        public async Task<IActionResult> Index(int? categoryId, string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            string userId = _userManager.GetUserId(User)!;

            // List has IEnumerable implemented as an interface
            // TODO: Get the contacts from the AppUser
            List<Contact> contacts = new List<Contact>();

            //Get the categories from AppUser based on whether they have chosen a category to "filter" by
            List<Category> categories = await _context.Categories
                                                      .Where(c => c.AppUserId == userId)
                                                      .ToListAsync();

            if (categoryId == null)
            {
                // Eager Load my data -use .Include() to ask for Categories- categories weren't showing up so we have to ask for it
                // query to get contacts where the app user id equals the user id we have(filters the data), anytime we talk to the database use ToListAsync
                contacts = await _context.Contacts
                                         .Where(c => c.AppUserId == userId)
                                         .Include(c => c.Categories)
                                         .ToListAsync();
            }
            else
            {              // await in parentheses allows the thread to run
                contacts = (await _context.Categories
                                         .Include(c => c.Contacts)
                                         .FirstOrDefaultAsync(c => c.AppUserId == userId && c.Id == categoryId))!
                                         .Contacts.ToList();
                                         // grabs the contacts from the single category we queried for
            }

           
                                                               // categoryId shows which category is selected
            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", categoryId);

            return View(contacts);
        }

        // POST: SearchContacts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchContacts(string? searchString)
        {
            string? userId = _userManager.GetUserId(User)!;

            List<Contact> contacts = new List<Contact>();


            AppUser? appUser = await _context.Users.Include(u => u.Contacts)
                                                      .ThenInclude(c => c.Categories)
                                                   .FirstOrDefaultAsync(u => u.Id == userId);

            if (string.IsNullOrEmpty(searchString))
            {
                contacts = appUser!.Contacts
                                   .OrderBy(c => c.LastName)
                                   .ThenBy(c => c.FirstName)
                                   .ToList();
            }
            else
            {
                contacts = appUser!.Contacts
                                    .Where(c => c.FullName!.ToLower().Contains(searchString.ToLower()))
                                    .OrderBy(c => c.LastName)
                                    .ThenBy(c => c.FirstName)
                                    .ToList();
            }

            // Need this so we still have our category filter active
            ViewData["CategoryId"] = new SelectList(appUser.Categories, "Id", "Name");
            return View(nameof(Index),contacts);
        }


        // GET: EmailContact
        public async Task<IActionResult> EmailContact(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            string? userId = _userManager.GetUserId(User);

            // same as doing a Where clause first to filter and then doing FirstOrDefaultAsync. But that's more complex than necessary
            Contact? contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == userId);


            if (contact == null)
            {
                return NotFound();
            }


            // Instantiate EmailData
            EmailData emailData = new EmailData()
            {
                EmailAddress = contact!.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
            };

            // Instantiate the ViewModel
            EmailContactView viewModel = new EmailContactView()
            {
                Contact = contact,
                EmailData = emailData
            };

            // Need to Make Sure to Return the viewModel we want to see
            return View(viewModel);
        }

        // POST: EmailContact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailContact(EmailContactView viewModel)
        {
            // IsValid = All required fields are checked
            if (ModelState.IsValid)
            {
                string? swalMessage = string.Empty;

                try
                {
                    await _emailService.SendEmailAsync(viewModel.EmailData!.EmailAddress!,
                                                       viewModel.EmailData.EmailSubject!,
                                                       viewModel.EmailData.EmailBody!);

                    swalMessage = "Your Email Has been Sent";

                    // Takes us back to the index after we send an email
                    // nameof makes it so that it has to reference something specific in our code instead of using a string that can be anything.
                    // this is how we let rosalind do her thing ^
                    return RedirectToAction(nameof(Index),new { swalMessage });
                    // new is a route value that sends a parameter 
                }
                catch (Exception)
                {
                    swalMessage = "Error: Email Send Failed";
                    return RedirectToAction(nameof(Index), new { swalMessage });
                    throw;
                }
            }


            return View(viewModel);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,BirthDate,Address1,Address2,City,States,ZipCode,Email,PhoneNumber,Created,ImageData,ImageType,ImageFile,AppUserId")] Contact contact, IEnumerable<int> selected, string? returnView = null)
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

            ViewData["returnView"] = returnView;
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
