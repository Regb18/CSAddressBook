namespace CSAddressBook.Models.ViewModels
{
    public class EmailContactView
    {
        public Contact? Contact { get; set; }

        // who the email goes to, the body, the subject are in here
        public EmailData? EmailData { get; set; }
    }
}
