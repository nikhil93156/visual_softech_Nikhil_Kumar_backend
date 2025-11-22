namespace StudentManagement.Model // or Models
{
    public class AuthRequest
    {
        // Initialize with empty string to prevent "Non-nullable property" warnings
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}