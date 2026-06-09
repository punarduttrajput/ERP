// Generate BCrypt hashes for seeded passwords (work factor 10 for speed)
var passwords = new[]
{
    ("superadmin@erp-platform.com", "SuperAdmin@123"),
    ("admin@mit.edu",               "Admin@123456"),
    ("faculty@mit.edu",             "Faculty@123456"),
    ("student@mit.edu",             "Student@123456")
};

foreach (var (email, pwd) in passwords)
{
    var hash = BCrypt.Net.BCrypt.HashPassword(pwd, workFactor: 10);
    Console.WriteLine($"-- {email}");
    Console.WriteLine($"UPDATE users SET PasswordHash = '{hash}' WHERE Email = '{email}';");
    Console.WriteLine();
}
