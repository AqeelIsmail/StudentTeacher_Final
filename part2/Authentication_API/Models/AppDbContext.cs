using Authentication_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Authentication_API
{
    public class AppDbContext : DbContext //AppDbContext inherits from DbContext,
                                          //which is the base class for working with Entity Framework Core.
                                          //It provides the necessary functionality to interact with the database.
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        //this constructor receives the database configuration through Dependency Injection.
        //for example, the configuration can specify SQL Server and the connection string.

        //entities
        public DbSet<User> Users { get; set; } //A DbSet<User> represents a collection of User entities that
                                               //EF Core can query, insert, update and delete.
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<CourseCategory> CourseCategories { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<CourseResult> CourseResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) 
            //This method is called by EF Core when it builds the database model.
        {
            base.OnModelCreating(modelBuilder);//This allows any configuration from the parent DbContext class to run.

            //Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.UserId);

                entity.HasIndex(u => u.Email).IsUnique();

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Users_Role",
                    "[Role] IN ('Student', 'Teacher')"));
            });

            //Student Profiles
            modelBuilder.Entity<StudentProfile>(entity =>
            {
                entity.ToTable("Student_Profiles");
                entity.HasKey(s => s.StudentProfileId);

                entity.HasIndex(s => s.StudentNumber).IsUnique();

                entity.HasOne(s => s.User)
                    .WithOne(u => u.StudentProfile)
                    .HasForeignKey<StudentProfile>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Teacher Profiles
            modelBuilder.Entity<TeacherProfile>(entity =>
            {
                entity.ToTable("Teacher_Profiles");
                entity.HasKey(t => t.TeacherProfileId);

                entity.HasIndex(t => t.EmployeeNumber).IsUnique();

                entity.HasOne(t => t.User)
                    .WithOne(u => u.TeacherProfile)
                    .HasForeignKey<TeacherProfile>(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Course Categories
            modelBuilder.Entity<CourseCategory>(entity =>
            {
                entity.ToTable("Course_Categories");
                entity.HasKey(c => c.CourseCategoryId);

                entity.HasIndex(c => c.Name).IsUnique();
            });

            //Courses
            modelBuilder.Entity<Course>(entity => //This tells EF Core that we're configuring the Course entity
            {
                entity.ToTable("Courses");
                entity.HasKey(c => c.CourseId);

                entity.HasIndex(c => c.Code).IsUnique(); //creates an index on the code column and enforces uniqueness,
                                                         //ensuring that no two courses can have the same code.

                entity.Property(c => c.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(c => c.TeacherProfile)//hasOne()-->A Course has one Teacher
                    .WithMany()//--> Teacher can have many Courses
                    .HasForeignKey(c => c.TeacherProfileId) //TeacherProfileId in Course is a foreign key 
                                                               //connects the two tables
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.CourseCategory)
                    .WithMany(cat => cat.Courses)
                    .HasForeignKey(c => c.CourseCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Enrolments
            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.ToTable("Enrolments");
                entity.HasKey(e => e.EnrolmentId);

                entity.HasIndex(e => new
                {
                    e.StudentProfileId,
                    e.CourseId
                }).IsUnique();

                entity.Property(e => e.EnrolledAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.Status)
                    .HasDefaultValue("Pending");

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Enrolments_Status",
                    "[Status] IN ('Pending', 'Confirmed', 'Cancelled')"));

                entity.HasOne(e => e.StudentProfile)
                    .WithMany()
                    .HasForeignKey(e => e.StudentProfileId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrolments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //Course Results
            modelBuilder.Entity<CourseResult>(entity =>
            {
                entity.ToTable("Course_Results");
                entity.HasKey(r => r.CourseResultId);

                entity.Property(r => r.FinalMark)
                    .HasPrecision(5, 2);

                entity.Property(r => r.RecordedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.HasIndex(r => r.EnrolmentId).IsUnique();

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_CourseResults_FinalMark",
                    "[FinalMark] BETWEEN 0 AND 100"));

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_CourseResults_Outcome",
                    "([FinalMark] >= 50 AND [Outcome] = 'Pass') OR " +
                    "([FinalMark] < 50 AND [Outcome] = 'Fail')"));

                entity.HasOne(r => r.Enrolment)
                    .WithOne(e => e.CourseResult)
                    .HasForeignKey<CourseResult>(r => r.EnrolmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
