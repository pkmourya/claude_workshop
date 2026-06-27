// UserNameFormatterTests.cs
public class UserNameFormatterTests
{
[Fact]
{
public void FormatUserName_WithMiddleName_ReturnsCorrectFormat()
var result = UserNameFormatter.FormatUserName("Jane", "Doe",
"Marie");
Assert.Equal("Doe, Jane M.", result);
}
[Fact]
{
public void FormatUserName_WithoutMiddleName_ReturnsFirstAndLastOnly()
var result = UserNameFormatter.FormatUserName("Jane", "Doe", null);
Assert.Equal("Doe, Jane", result); // no trailing space, no dot
}
}