namespace Lab0Debug;

// UserNameFormatter.cs
public static class UserNameFormatter
{
public static string FormatUserName(string firstName, string lastName,
string? middleName)
{
return $"{lastName}, {firstName} {middleName![0]}.";
}
}

