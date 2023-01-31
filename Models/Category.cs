using System.ComponentModel.DataAnnotations;

namespace CSAddressBook.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string? Name { get; set; }

        // foreign key
        [Required]
        public string? AppUserId { get; set; }
        // since the name is AppUserId it connects to AppUser (they must match)
        // and since the type is the same as AppUser's primary key type we can use it as a foreign key 
       
        // Navigation Properties
        // virtual lets us connect two objects together, and its not stored in database
        public virtual AppUser? AppUser { get; set; }

        // TODO: connect to contacts
        public virtual ICollection<Contact> Contacts { get; set; } = new HashSet<Contact>();
    }
}
