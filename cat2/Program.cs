using System;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MySql.Data.MySqlClient;

class Student
{
    public string StudentID { get; set; }
    public string Name { get; set; }
    public Dictionary<string, double> Units { get; set; }

    private const string ConnectionString = "server=localhost;database=students;user=root;password=;";

    public Student(string studentID, string name)
    {
        StudentID = studentID;
        Name = name;
        Units = new Dictionary<string, double>();
    }

    public void AddSubject(string unit, double marks)
    {
        Units[unit] = marks;
    }

    public double GetTotalMarks()
    {
        double total = 0;
        foreach (var marks in Units.Values)
        {
            total += marks;
        }
        return total;
    }

    public double GetAverageMarks()
    {
        return Units.Count == 0 ? 0 : GetTotalMarks() / Units.Count;
    }

    public string GetGrade()
    {
        double average = GetAverageMarks();
        if (average >= 90) return "A";
        if (average >= 80) return "B";
        if (average >= 70) return "C";
        if (average >= 60) return "D";
        return "F";
    }

    public void SaveToDatabase()
    {
        using (MySqlConnection conn = new MySqlConnection(ConnectionString))
        {
            conn.Open();

            string insertStudent = @"INSERT INTO students (student_id, name) 
                                    VALUES (@id, @name) 
                                    ON DUPLICATE KEY UPDATE name=@name;";
            using (MySqlCommand cmd = new MySqlCommand(insertStudent, conn))
            {
                cmd.Parameters.AddWithValue("@id", StudentID);
                cmd.Parameters.AddWithValue("@name", Name);
                cmd.ExecuteNonQuery();
            }

            foreach (var unit in Units)
            {
                string insertMarks = @"INSERT INTO results (student_id, unit, marks) 
                                       VALUES (@id, @unit, @marks) 
                                       ON DUPLICATE KEY UPDATE marks=@marks;";
                using (MySqlCommand cmd = new MySqlCommand(insertMarks, conn))
                {
                    cmd.Parameters.AddWithValue("@id", StudentID);
                    cmd.Parameters.AddWithValue("@unit", unit.Key);
                    cmd.Parameters.AddWithValue("@marks", unit.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        Console.WriteLine("Student results saved successfully.");
    }

    public static Student RetrieveFromDatabase(string studentID)
    {
        Student student = null;
        using (MySqlConnection conn = new MySqlConnection(ConnectionString))
        {
            conn.Open();

            // Retrieve student info
            string selectStudent = "SELECT name FROM students WHERE student_id=@id;";
            using (MySqlCommand cmd = new MySqlCommand(selectStudent, conn))
            {
                cmd.Parameters.AddWithValue("@id", studentID);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        student = new Student(studentID, reader.GetString("name"));
                    }
                }
            }

            // Retrieve marks if student exists
            if (student != null)
            {
                string selectMarks = "SELECT unit, marks FROM results WHERE student_id=@id;";
                using (MySqlCommand cmd = new MySqlCommand(selectMarks, conn))
                {
                    cmd.Parameters.AddWithValue("@id", studentID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            student.AddSubject(
                                reader.GetString("unit"),
                                reader.GetDouble("marks")
                            );
                        }
                    }
                }
            }
        }
        return student;
    }

    public void SaveResultSlipToPDF(string filePath)
    {
        // Consider using a relative path or configurable location
        string logoPath = @"C:\Users\HP\Desktop\ml\logo.png";  // Update this path

        Document doc = new Document();
        PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
        doc.Open();

        // Add logo
        if (File.Exists(logoPath))
        {
            Image logo = Image.GetInstance(logoPath);
            logo.ScaleToFit(100f, 100f);
            logo.Alignment = Element.ALIGN_CENTER;
            doc.Add(logo);
        }
        else
        {
            Console.WriteLine("Warning: Logo file not found at " + logoPath);
        }

        // Add header
        Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLUE);
        Paragraph title = new Paragraph("KCA UNIVERSITY\nStudent Result Slip", titleFont);
        title.Alignment = Element.ALIGN_CENTER;
        doc.Add(title);
        doc.Add(new Paragraph("\n"));

        // Add student info
        Font infoFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.BLACK);
        doc.Add(new Paragraph($"Student ID: {StudentID}", infoFont));
        doc.Add(new Paragraph($"Name: {Name}", infoFont));
        doc.Add(new Paragraph("\n"));

        // Create marks table
        PdfPTable table = new PdfPTable(2);
        table.WidthPercentage = 100;
        table.SetWidths(new float[] { 70, 30 });

        // Table headers
        table.AddCell(new PdfPCell(new Phrase("Unit", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))));
        table.AddCell(new PdfPCell(new Phrase("Marks", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))));

        // Table data
        foreach (var unit in Units)
        {
            table.AddCell(new Phrase(unit.Key, infoFont));
            table.AddCell(new Phrase(unit.Value.ToString(), infoFont));
        }

        doc.Add(table);
        doc.Add(new Paragraph("\n"));

        // Add summary
        doc.Add(new Paragraph($"Total Marks: {GetTotalMarks()}", infoFont));
        doc.Add(new Paragraph($"Average Marks: {GetAverageMarks():F2}", infoFont));
        doc.Add(new Paragraph($"Grade: {GetGrade()}",
            FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.RED)));

        doc.Close();
        Console.WriteLine($"Result slip saved to: {filePath}");
    }
}

class Program
{
    static void Main()
    {
        while (true)
        {
            Console.WriteLine("Choose an operation:");
            Console.WriteLine("1. Add Student Results");
            Console.WriteLine("2. Generate Result Slip");
            Console.WriteLine("3. Exit");
            Console.Write("Enter choice: ");

            try
            {
                int choice = int.Parse(Console.ReadLine());

                if (choice == 1)
                {
                    Console.Write("Enter Student ID: ");
                    string id = Console.ReadLine();

                    Console.Write("Enter Student Name: ");
                    string name = Console.ReadLine();

                    Student student = new Student(id, name);

                    Console.Write("Number of units: ");
                    int unitCount = int.Parse(Console.ReadLine());

                    for (int i = 0; i < unitCount; i++)
                    {
                        Console.Write($"Unit {i + 1} Name: ");
                        string unit = Console.ReadLine();

                        Console.Write($"Marks for {unit}: ");
                        double marks = double.Parse(Console.ReadLine());

                        student.AddSubject(unit, marks);
                    }

                    student.SaveToDatabase();
                }
                else if (choice == 2)
                {
                    Console.Write("Enter Student ID: ");
                    string id = Console.ReadLine();

                    Student student = Student.RetrieveFromDatabase(id);

                    if (student != null)
                    {
                        string pdfPath = @"C:\\Users\\HP\\Desktop\\" + $"{student.StudentID}_ResultSlip.pdf";
                        student.SaveResultSlipToPDF(pdfPath);
                    }
                    else
                    {
                        Console.WriteLine("Student record not found!");
                    }
                }
                else if (choice == 3)
                {
                    Console.WriteLine("Exiting program. Goodbye!");
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid choice! Please enter 1, 2, or 3.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
