using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSAddressBook.Models
{
    public class AppUser : IdentityUser
    {
        //annotation that makes the input required
        [Required]
        //decorator annotation that changes how this input looks on the page
        [Display(Name = "First Name")]
        //annotation that limits the user in what they can enter, max characters = 50 min = 2 
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
        public string? FirstName { get; set; }

        [Required]
        [Display(Name ="Last Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
        public string? LastName { get; set; }

        //annotation that makes this property not grab anything from database
        [NotMapped]
        public string? FullName { get { return $"{FirstName} {LastName}"; } }


        //Navigation Properties
        public virtual ICollection<Category> Categories { get; set; } = new HashSet<Category>();
        public virtual ICollection<Contact> Contacts { get; set; } = new HashSet<Contact>();    

    }
}
