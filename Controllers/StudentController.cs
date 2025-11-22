using Microsoft.AspNetCore.Mvc;
using StudentManagement.Models;
using StudentManagement.Services;

namespace StudentManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly SqlDataAccessService _dataService;

        public StudentsController(SqlDataAccessService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _dataService.GetAllStudentsAsync();
            return Ok(students);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _dataService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();
            return Ok(student);
        }

        // POST: api/Students
        [HttpPost]
        public async Task<IActionResult> PostStudent([FromForm] Student student) // <--- CHANGE THIS TO [FromForm]
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // CONVERT IMAGE TO BYTES
            if (student.Photo != null && student.Photo.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await student.Photo.CopyToAsync(memoryStream);
                    student.PhotoData = memoryStream.ToArray();
                }
            }

            var studentId = await _dataService.AddStudentAsync(student);
            return CreatedAtAction(nameof(GetStudent), new { id = studentId }, student);
        }

        // PUT: api/Students/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudent(int id, [FromForm] Student student) // <--- CHANGE THIS TO [FromForm]
        {
            if (id != student.StudentId) return BadRequest();

            // CONVERT IMAGE TO BYTES (If a NEW photo was uploaded)
            if (student.Photo != null && student.Photo.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await student.Photo.CopyToAsync(memoryStream);
                    student.PhotoData = memoryStream.ToArray();
                }
            }
            // Note: If Photo is null, your DataAccessService should ideally keep the OLD PhotoData
            // logic depending on how your Update SQL query is written.

            var success = await _dataService.UpdateStudentAsync(student);
            if (!success) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var success = await _dataService.DeleteStudentAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        // --- State Dropdown APIs (unchanged) ---
        [HttpGet("states")]
        public async Task<IActionResult> GetStates()
        {
            var states = await _dataService.GetAllStatesAsync();
            return Ok(states);
        }

        [HttpPost("states")]
        public async Task<IActionResult> PostState([FromBody] State state)
        {
            var newId = await _dataService.SaveNewStateAsync(state.StateName);
            if (newId == -1) return BadRequest(new { message = "State already exists." });
            return Ok(new { StateId = newId, StateName = state.StateName });
        }
    }
}