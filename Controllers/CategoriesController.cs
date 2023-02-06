using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CSAddressBook.Data;
using CSAddressBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CSAddressBook.Services.Interfaces;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace CSAddressBook.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IAddressBookService _addressBookService;
        private readonly IEmailSender _emailService;
        public CategoriesController(ApplicationDbContext context, 
                                    UserManager<AppUser> userManager, 
                                    IAddressBookService addressBookService,
                                    IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _addressBookService = addressBookService;
            _emailService = emailService;
        }

        // GET: Categories
        public async Task<IActionResult> Index(string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            string userId = _userManager.GetUserId(User)!;


            // List has IEnumerable implemented as an interface
            List<Category> categories = new List<Category>();

            // to get categories where the app user id equals the user id we have(filters the data), anytime we talk to the database use ToListAsync
            // _context talks to the database to get everything from the Categories table
            // Where is a lamda expression - "use c to go into Categories table to find AppUserId fields that equal userId
            categories = await _context.Categories.Where(c => c.AppUserId == userId).Include(c => c.Contacts).ToListAsync();

            return View(categories);
        }

        // GET: EmailCategory
        public async Task<IActionResult> EmailCategory(int? id, string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            if (id == null)
            {
                return NotFound();
            }



            string? userId = _userManager.GetUserId(User);
            Category? category = await _context.Categories
                                               .Include(c => c.Contacts)
                                               .FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == userId);

        
            if (category == null)
            {
                return NotFound();
            }


            // Looks at each contact in the category and grabs the emails, adds them to the list
            List<string> emails = category!.Contacts.Select(c => c.Email).ToList()!;

            EmailData emailData = new EmailData()
            {
                GroupName = category.Name,
                // the semicolon separates each category name -- it is a delimiter
                EmailAddress = string.Join(";", emails),
                EmailSubject = $"Group Message: {category.Name}"
            };


            return View(emailData);
        }

        // POST: EmailCategory

        // EmailCategoryView viewModel -- return View(viewModel)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailCategory(EmailData emailData)
        {

            if (ModelState.IsValid)
            {
                    string? swalMessage = string.Empty;
                try
                {
                    await _emailService.SendEmailAsync(emailData.EmailAddress!,
                                                       emailData.EmailSubject!,
                                                       emailData.EmailBody!);

                    swalMessage = "Your Email Has been Sent";
                    return RedirectToAction(nameof(Index), new { swalMessage });
                }
                catch (Exception)
                {
                    swalMessage = "Error: Email Send Failed";
                    return RedirectToAction(nameof(EmailCategory), new { swalMessage });
                    throw;
                }
            }

            return View(emailData);
        }



        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            //
            // Fix Security on this Page, can get to other user's details if I know the URL
            // FIXED!

            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }


            string userId = _userManager.GetUserId(User)!;

            // TEST: see if this works without the categories variable lines

            List<Category> categories = new List<Category>();
            categories = await _context.Categories.Include(c => c.Contacts).ToListAsync();

            var category = await _context.Categories
                                         .Include(c => c.AppUser).Where(c => c.AppUserId == userId)
                                         .FirstOrDefaultAsync(m => m.Id == id);

            //var category = await _context.Categories
            //                             .Where(c => c.AppUserId == userId)
            //                             .Include(c => c.Contacts)
            //                             .FirstOrDefaultAsync(m => m.Id == id);



            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public async Task<IActionResult> CreateAsync()
        {
            string? userId = _userManager.GetUserId(User);

            // Query and present list of Contacts for logged in user
            IEnumerable<Contact> contactsList = await _context.Contacts
                                                                 .Where(c => c.AppUserId == userId)
                                                                 .ToListAsync();

            // 3rd parameter is what we want to see in the list - dataTextField
            // 2nd parameter is what I want to get when I select multiple of those names
            ViewData["ContactList"] = new MultiSelectList(contactsList, "Id", "FullName");

            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,AppUserId")] Category category, IEnumerable<int> selected)
        {

            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                category.AppUserId = _userManager.GetUserId(User);

                _context.Add(category);
                await _context.SaveChangesAsync();

                // call Service
                await _addressBookService.AddCategoryToContactsAsync(selected, category.Id);

                return RedirectToAction(nameof(Index));
            }
            //ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", category.AppUserId);
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }
            var category = await _context.Categories
                                         .Include(c => c.Contacts)
                                         .FirstOrDefaultAsync(c => c.Id == id);


            string? userId = _userManager.GetUserId(User);

            IEnumerable<Contact> contactsList = await _context.Contacts
                                                                 .Where(c => c.AppUserId == userId)
                                                                 .ToListAsync();

            IEnumerable<int> currentContacts = category!.Contacts.Select(c => c.Id);

            ViewData["ContactList"] = new MultiSelectList(contactsList, "Id", "FullName", currentContacts);

            if (category == null)
            {
                return NotFound();
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", category.AppUserId);
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,AppUserId")] Category category, IEnumerable<int> selected)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    //
                    //TODO: Add Services
                    //DONE!

                    if (selected != null)
                    {
                        // 1. Remove Category's contacts
                        await _addressBookService.RemoveAllCategoryContactsAsync(category.Id);

                        // 2. Add selected contacts to the category
                        await _addressBookService.AddCategoryToContactsAsync(selected, category.Id);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
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
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", category.AppUserId);
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Categories == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Categories'  is null.");
            }
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
          return (_context.Categories?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
