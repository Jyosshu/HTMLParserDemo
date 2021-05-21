
namespace ClassLibrary
{
    public class CreatorAuthor
    {
        public int CreatorAuthorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FirstName_English { get; set; }
        public string LastName_English { get; set; }
        public string ContryOfOrigin { get; set; }

        public string FullName()
        {
            return $"{LastName} {FirstName}";
        }
        
        public string FullNameEnglish()
        {
            return $"{FirstName_English} {LastName_English}";
        }
    }
}
