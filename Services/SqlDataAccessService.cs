using System.Data;
using Microsoft.Data.SqlClient; 
using StudentManagement.Models;
using Microsoft.Extensions.Configuration;

namespace StudentManagement.Services
{
    public class SqlDataAccessService
    {
        private readonly string _connectionString;

        public SqlDataAccessService(IConfiguration configuration)
        {
            // Fix CS8618/CS8601: Ensure connection string is never null
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        // ---------------------------------------------------------
        // 0. AUTHENTICATION (FIXED THE BUILD ERROR)
        // ---------------------------------------------------------
        public bool AuthenticateUser(string username, string password)
        {
            // For now, we keep this simple to fix the build error.
            // In the future, you should create a 'Users' table in SQL and query it here.
            return username == "admin" && password == "password123";
        }

        // ---------------------------------------------------------
        // 1. GET ALL STUDENTS
        // ---------------------------------------------------------
        public async Task<List<Student>> GetAllStudentsAsync()
        {
            var students = new List<Student>();
            
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT s.StudentId, s.Name, s.Age, s.DateOfBirth, s.Address, s.PhoneNumber, 
                           s.StateId, st.StateName, s.PhotoData
                    FROM Student_Mast s
                    INNER JOIN State_Mast st ON s.StateId = st.StateId
                    ORDER BY s.StudentId DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var student = new Student
                        {
                            // Fix CS8605: Safely cast DB objects
                            StudentId = reader["StudentId"] != DBNull.Value ? (int)reader["StudentId"] : 0,
                            Name = reader["Name"]?.ToString(),
                            Age = reader["Age"] != DBNull.Value ? (int)reader["Age"] : 0,
                            DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? (DateTime)reader["DateOfBirth"] : DateTime.MinValue,
                            Address = reader["Address"]?.ToString(),
                            PhoneNumber = reader["PhoneNumber"]?.ToString(),
                            StateId = reader["StateId"] != DBNull.Value ? (int)reader["StateId"] : 0,
                            StateName = reader["StateName"]?.ToString(),
                            PhotoData = reader["PhotoData"] == DBNull.Value ? null : (byte[])reader["PhotoData"]
                        };
                        students.Add(student);
                    }
                }

                foreach (var stud in students)
                {
                    stud.Subjects = await GetSubjectsForStudentAsync(stud.StudentId, conn);
                }
            }
            return students;
        }

        // ---------------------------------------------------------
        // 2. GET SINGLE STUDENT
        // ---------------------------------------------------------
        // Fix CS8603: Allow returning null (Student?) if not found
        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            Student? student = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT s.StudentId, s.Name, s.Age, s.DateOfBirth, s.Address, s.PhoneNumber, 
                           s.StateId, st.StateName, s.PhotoData
                    FROM Student_Mast s
                    INNER JOIN State_Mast st ON s.StateId = st.StateId
                    WHERE s.StudentId = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            student = new Student
                            {
                                StudentId = (int)reader["StudentId"],
                                Name = reader["Name"]?.ToString(),
                                Age = (int)reader["Age"],
                                DateOfBirth = (DateTime)reader["DateOfBirth"],
                                Address = reader["Address"]?.ToString(),
                                PhoneNumber = reader["PhoneNumber"]?.ToString(),
                                StateId = (int)reader["StateId"],
                                StateName = reader["StateName"]?.ToString(),
                                PhotoData = reader["PhotoData"] == DBNull.Value ? null : (byte[])reader["PhotoData"]
                            };
                        }
                    }
                }

                if (student != null)
                {
                    student.Subjects = await GetSubjectsForStudentAsync(student.StudentId, conn);
                }
            }
            return student;
        }

        // ---------------------------------------------------------
        // 3. CREATE STUDENT
        // ---------------------------------------------------------
        public async Task<int> AddStudentAsync(Student student)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string insertMast = @"
                            INSERT INTO Student_Mast (Name, Age, DateOfBirth, Address, StateId, PhoneNumber, PhotoData) 
                            OUTPUT INSERTED.StudentId 
                            VALUES (@Name, @Age, @Dob, @Addr, @StateId, @Phone, @Photo)";

                        int newId;
                        using (SqlCommand cmd = new SqlCommand(insertMast, conn, transaction))
                        {
                            // Fix CS8604: Handle possible null values
                            cmd.Parameters.AddWithValue("@Name", student.Name ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Age", student.Age);
                            cmd.Parameters.AddWithValue("@Dob", student.DateOfBirth);
                            cmd.Parameters.AddWithValue("@Addr", student.Address ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@StateId", student.StateId);
                            cmd.Parameters.AddWithValue("@Phone", student.PhoneNumber ?? (object)DBNull.Value);
                            cmd.Parameters.Add("@Photo", SqlDbType.VarBinary).Value = (object?)student.PhotoData ?? DBNull.Value;

                            // Fix CS8605: Unboxing null result
                            var res = await cmd.ExecuteScalarAsync();
                            newId = res != null ? (int)res : 0;
                        }

                        if (student.Subjects != null && student.Subjects.Count > 0)
                        {
                            foreach (var sub in student.Subjects)
                            {
                                string insertSub = "INSERT INTO Student_Detail (StudentId, SubjectName) VALUES (@Sid, @SName)";
                                using (SqlCommand cmdSub = new SqlCommand(insertSub, conn, transaction))
                                {
                                    cmdSub.Parameters.AddWithValue("@Sid", newId);
                                    cmdSub.Parameters.AddWithValue("@SName", sub);
                                    await cmdSub.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await transaction.CommitAsync();
                        return newId;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        // ---------------------------------------------------------
        // 4. UPDATE STUDENT
        // ---------------------------------------------------------
        public async Task<bool> UpdateStudentAsync(Student student)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string updateQuery = @"
                            UPDATE Student_Mast 
                            SET Name=@Name, Age=@Age, DateOfBirth=@Dob, Address=@Addr, 
                                StateId=@StateId, PhoneNumber=@Phone, PhotoData=@Photo
                            WHERE StudentId=@Id";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", student.StudentId);
                            cmd.Parameters.AddWithValue("@Name", student.Name ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Age", student.Age);
                            cmd.Parameters.AddWithValue("@Dob", student.DateOfBirth);
                            cmd.Parameters.AddWithValue("@Addr", student.Address ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@StateId", student.StateId);
                            cmd.Parameters.AddWithValue("@Phone", student.PhoneNumber ?? (object)DBNull.Value);
                            cmd.Parameters.Add("@Photo", SqlDbType.VarBinary).Value = (object?)student.PhotoData ?? DBNull.Value;

                            int rows = await cmd.ExecuteNonQueryAsync();
                            if (rows == 0) return false;
                        }

                        string deleteSub = "DELETE FROM Student_Detail WHERE StudentId = @Id";
                        using (SqlCommand cmdDel = new SqlCommand(deleteSub, conn, transaction))
                        {
                            cmdDel.Parameters.AddWithValue("@Id", student.StudentId);
                            await cmdDel.ExecuteNonQueryAsync();
                        }

                        if (student.Subjects != null && student.Subjects.Count > 0)
                        {
                            foreach (var sub in student.Subjects)
                            {
                                string insertSub = "INSERT INTO Student_Detail (StudentId, SubjectName) VALUES (@Sid, @SName)";
                                using (SqlCommand cmdSub = new SqlCommand(insertSub, conn, transaction))
                                {
                                    cmdSub.Parameters.AddWithValue("@Sid", student.StudentId);
                                    cmdSub.Parameters.AddWithValue("@SName", sub);
                                    await cmdSub.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        // ---------------------------------------------------------
        // 5. DELETE STUDENT
        // ---------------------------------------------------------
        public async Task<bool> DeleteStudentAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "DELETE FROM Student_Mast WHERE StudentId = @Id";
                
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0;
                }
            }
        }

        // ---------------------------------------------------------
        // 6. STATE OPERATIONS
        // ---------------------------------------------------------
        public async Task<List<State>> GetAllStatesAsync()
        {
            var states = new List<State>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT StateId, StateName FROM State_Mast";
                
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        states.Add(new State
                        {
                            StateId = reader["StateId"] != DBNull.Value ? (int)reader["StateId"] : 0,
                            StateName = reader["StateName"]?.ToString()
                        });
                    }
                }
            }
            return states;
        }

        // Fix CS8604: handle null stateName
        public async Task<int> SaveNewStateAsync(string? stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName)) return -1;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                
                string checkQuery = "SELECT COUNT(1) FROM State_Mast WHERE StateName = @Name";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Name", stateName);
                    var res = await checkCmd.ExecuteScalarAsync();
                    int exists = res != null ? (int)res : 0;
                    if (exists > 0) return -1;
                }

                string insertQuery = "INSERT INTO State_Mast (StateName) OUTPUT INSERTED.StateId VALUES (@Name)";
                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", stateName);
                    var res = await cmd.ExecuteScalarAsync();
                    return res != null ? (int)res : 0;
                }
            }
        }

        private async Task<List<string>> GetSubjectsForStudentAsync(int studentId, SqlConnection conn)
        {
            var subs = new List<string>();
            string query = "SELECT SubjectName FROM Student_Detail WHERE StudentId = @Id";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Id", studentId);
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var sName = reader["SubjectName"]?.ToString();
                        // Fix CS8604: Don't add nulls to the list
                        if (sName != null) subs.Add(sName);
                    }
                }
            }
            return subs;
        }
    }
}