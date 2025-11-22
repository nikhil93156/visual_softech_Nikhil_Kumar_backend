using Microsoft.AspNetCore.Http; // REQUIRED for IFormFile
using System.ComponentModel.DataAnnotations.Schema; // REQUIRED for [NotMapped]

namespace StudentManagement.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Address { get; set; }
        public int StateId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? StateName { get; set; }
        public List<string> Subjects { get; set; } = new List<string>();
        
        // Stores binary data in DB
        public byte[]? PhotoData { get; set; }

        // RECEIVES the file from React
        [NotMapped]
        public IFormFile? Photo { get; set; }
    }

    public class State
    {
        public int StateId { get; set; }
        public string? StateName { get; set; }
    }
}