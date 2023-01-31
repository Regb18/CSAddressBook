using CSAddressBook.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSAddressBook.Models
{
    public class Contact
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
        public string? FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
        public string? LastName { get; set; }

        [NotMapped]
        public string? FullName { get { return $"{FirstName} {LastName}"; } }

        [Display(Name = "Birthday")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string? Address1 { get; set; }

        [Display(Name = "Address 2")]
        public string? Address2 { get; set; }

        [Required]
        public string? City { get; set; }

        [Required]
        public States States { get; set; }


        // int doesn't need question mark because it can't be null without one, so it's required by default
        [Display(Name = "Zip Code")]
        [DataType(DataType.PostalCode)]
        public int ZipCode { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }


        // by default a date time has a value, but if it's required we won't accept the default value
        [Required]
        [DataType(DataType.Date)]
        public DateTime Created { get; set; }

        public byte[]? ImageData { get; set; }
        public string? ImageType { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }



        [Required]
        public string? AppUserId { get; set; }

        // Navigation Properties
        public virtual AppUser? AppUser { get; set; }
        public virtual ICollection<Category> Categories { get; set; } = new HashSet<Category>();
    }
}
